using Grimoire.Game;
using Grimoire.Game.Data;
using Grimoire.UI;
using Grimoire.Botting.Commands.Combat;
using Grimoire.Botting.Commands.Map;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Grimoire.Botting.Commands.Quest
{
    /// <summary>
    /// Runs Captain Rolith's free Noobshire story chain from a fresh character.
    /// The command is deliberately story-scoped: it never accepts unrelated quests.
    /// </summary>
    public sealed class CmdNoobshireStory : IBotCommand
    {
        private sealed class StoryStep
        {
            public int QuestId { get; set; }
            public string Title { get; set; }
            public string Monster { get; set; }
            public string Item { get; set; }
            public int Quantity { get; set; }
            public string MapItemName { get; set; }
            public int MapItemQuantity { get; set; }
        }

        private static readonly StoryStep[] Steps =
        {
            new StoryStep { QuestId = 32, Title = "Defend the Rats", Monster = "Horc Noob", Item = "Piece of Cloth", Quantity = 8 },
            new StoryStep { QuestId = 33, Title = "Rats in the Cradle", Monster = "Horc Noob", Item = "Horc Cheese", Quantity = 1, MapItemName = "Baby Rat", MapItemQuantity = 6 },
            new StoryStep { QuestId = 35, Title = "Short in Spoons", Monster = "Kittarian Mouse Eater", Item = "Spoon", Quantity = 5 },
            new StoryStep { QuestId = 36, Title = "Short in Forks", Monster = "Kittarian Mouse Flayer", Item = "Fork", Quantity = 5 },
            new StoryStep { QuestId = 37, Title = "Letter Interception", Monster = "Horc Noob", Item = "Horc's Letter", Quantity = 1 },
            new StoryStep { QuestId = 38, Title = "Not a Noob", Monster = "Horc Trainer", Item = "Horc Trainer Horns", Quantity = 1 },
            new StoryStep { QuestId = 39, Title = "Missing King", Monster = "Horc Noob", Item = "Rat King Clue", Quantity = 1, MapItemName = "Rat Leader", MapItemQuantity = 1 }
        };

        public async Task Execute(IBotEngine instance)
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));

            BotData.BotState = BotData.State.Quest;
            Log("Starting the free Noobshire story (quests 32 to 39). Quest 40 is intentionally excluded because it is Legend-only.");

            await new CmdJoin2
            {
                Map = "noobshire",
                Room = "1e99",
                Cell = "Enter",
                Pad = "Spawn"
            }.Execute(instance);

            foreach (StoryStep step in Steps)
            {
                if (!instance.IsRunning)
                    return;

                Grimoire.Game.Data.Quest quest = await LoadQuest(instance, step.QuestId);
                if (quest == null)
                {
                    Log("Could not load quest " + step.QuestId + " (" + step.Title + "). Stopping to avoid unsafe progression.");
                    return;
                }

                if (!Player.Quests.IsInProgress(step.QuestId))
                {
                    if (quest.IsMemberOnly)
                    {
                        Log("Skipping member-only quest " + step.QuestId + " (" + step.Title + ").");
                        continue;
                    }

                    if (!quest.CanAccept)
                    {
                        Log("Quest " + step.QuestId + " (" + step.Title + ") is not available for this character. Skipping this story step.");
                        continue;
                    }

                    await instance.WaitUntil(() => World.IsActionAvailable(LockActions.AcceptQuest), null, 20, 500);
                    if (!instance.IsRunning)
                        return;

                    Player.AcceptQuest(step.QuestId);
                    bool accepted = await WaitFor(instance, () => Player.Quests.IsInProgress(step.QuestId), 20);
                    if (!accepted)
                    {
                        Log("The game did not confirm acceptance of quest " + step.QuestId + ". Stopping.");
                        return;
                    }
                }
                else
                {
                    Log("Resuming quest " + step.QuestId + " already in progress.");
                }

                if (!string.IsNullOrWhiteSpace(step.MapItemName) && step.MapItemQuantity > 0)
                {
                    bool mapObjectiveDone = HasItem(step.MapItemName, step.MapItemQuantity);
                    if (!mapObjectiveDone)
                    {
                        Log("Collecting map objective: " + step.MapItemName + " x" + step.MapItemQuantity + ".");
                        int attempts = await MapItemCollector.CollectForQuest(
                            step.QuestId,
                            item => IsMatchingMapItem(item, step.MapItemName),
                            () => HasItem(step.MapItemName, step.MapItemQuantity),
                            () => instance.IsRunning,
                            20,
                            60);

                        if (!instance.IsRunning)
                            return;

                        if (!HasItem(step.MapItemName, step.MapItemQuantity))
                        {
                            Log("Could not confirm " + step.MapItemName + " after " + attempts + " map-item click attempts. Stopping so the quest is not abandoned.");
                            return;
                        }
                    }
                }

                if (!string.IsNullOrWhiteSpace(step.Item) && step.Quantity > 0
                    && !HasItem(step.Item, step.Quantity))
                {
                    Log("Farming " + step.Item + " x" + step.Quantity + " from " + step.Monster + ".");
                    CmdKill kill = new CmdKill { Monster = step.Monster };
                    while (instance.IsRunning && !HasItem(step.Item, step.Quantity))
                    {
                        int before = GetItemQuantity(step.Item);
                        await kill.ExecuteForKillFor(instance, () => GetItemQuantity(step.Item) > before);
                        if (!Player.IsLoggedIn || !instance.IsRunning)
                            return;
                    }
                }

                if (!instance.IsRunning)
                    return;

                bool canTurnIn = await WaitFor(instance, () => Player.Quests.CanComplete(step.QuestId), 12);
                if (!canTurnIn)
                {
                    Log("Objectives for quest " + step.QuestId + " are not confirmed by the AQW client. Stopping before turn-in.");
                    return;
                }

                await instance.WaitUntil(() => World.IsActionAvailable(LockActions.TryQuestComplete), null, 20, 500);
                if (!instance.IsRunning)
                    return;

                Player.CompleteQuest(step.QuestId);
                bool completed = await WaitFor(instance, () => !Player.Quests.IsInProgress(step.QuestId), 20);
                if (!completed)
                {
                    Log("The game did not confirm turn-in of quest " + step.QuestId + ". Stopping.");
                    return;
                }

                Log("Completed quest " + step.QuestId + ": " + step.Title + ".");
            }

            Log("Noobshire free Rolith story sequence finished or all remaining unavailable steps were skipped.");
        }

        private static async Task<Grimoire.Game.Data.Quest> LoadQuest(IBotEngine instance, int questId)
        {
            Grimoire.Game.Data.Quest quest = Player.Quests.LoadedQuests.FirstOrDefault(q => q.Id == questId);
            if (quest != null)
                return quest;

            Player.Quests.GetQuest(questId);
            await WaitFor(instance, () => Player.Quests.LoadedQuests.Any(q => q.Id == questId), 20);
            return Player.Quests.LoadedQuests.FirstOrDefault(q => q.Id == questId);
        }

        private static async Task<bool> WaitFor(IBotEngine instance, Func<bool> condition, int checks)
        {
            for (int i = 0; i < checks && instance.IsRunning; i++)
            {
                if (condition())
                    return true;
                await Task.Delay(500);
            }
            return condition();
        }

        private static bool IsMatchingMapItem(MapItem item, string expectedName)
        {
            if (item == null)
                return false;

            string haystack = string.Join(" ", new[] { item.Name, item.ProcessMsg, item.CollectMsg }
                .Where(value => !string.IsNullOrWhiteSpace(value)));

            return haystack.IndexOf(expectedName, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool HasItem(string name, int quantity)
        {
            return GetItemQuantity(name) >= quantity;
        }

        private static int GetItemQuantity(string name)
        {
            int inventoryQuantity = Player.Inventory.Items?
                .Where(item => item != null && string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase))
                .Sum(item => item.Quantity) ?? 0;

            int tempQuantity = Player.TempInventory.Items?
                .Where(item => item != null && string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase))
                .Sum(item => item.Quantity) ?? 0;

            return inventoryQuantity + tempQuantity;
        }

        private static void Log(string message)
        {
            LogForm.Instance.AppendBot("[" + DateTime.Now.ToString("HH:mm:ss") + "] [Noobshire] " + message + "\r\n");
        }

        public override string ToString()
        {
            return "Story: Noobshire (free quests 32-39)";
        }
    }
}
