using Grimoire.Botting;
using Grimoire.Botting.Commands.Combat;
using Grimoire.Botting.Commands.Map;
using Grimoire.Game;
using Grimoire.Game.Data;
using GameQuest = Grimoire.Game.Data.Quest;
using Grimoire.UI;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Grimoire.Botting.Commands.Quest
{
    /// <summary>
    /// Repeats the three non-daily Swordhaven reputation quests in a private Castle room.
    /// Stops at Swordhaven rank 10 and never accepts unrelated quests.
    /// </summary>
    public sealed class CmdSwordhavenRep : IBotCommand
    {
        private sealed class Step
        {
            public int QuestId;
            public string Title;
            public string Monster;
            public string ItemOne;
            public int QuantityOne;
            public string ItemTwo;
            public int QuantityTwo;
            public bool KillCounter;
        }

        // Quest IDs are cross-checked against public Swordhaven quest references.
        // AQW client quest state is the authority for objective completion; no item-name guessing.
        private static readonly Step[] Steps =
        {
            new Step { QuestId = 3065, Title = "Boy Fiends and Ghoul Fiends", Monster = "Dungeon Fiend",
                ItemOne = "Dungeon Fiend Hair Bow", QuantityOne = 5,
                ItemTwo = "Dungeon Fiend Bow Tie", QuantityTwo = 5 },
            new Step { QuestId = 3066, Title = "Arachnid Eradication", Monster = "Castle Spider",
                ItemOne = "Eradicated Archnid", QuantityOne = 10 },
            new Step { QuestId = 3067, Title = "Apparition Nation", Monster = "Castle Wraith",
                ItemOne = "Castle Wraith Defeated", QuantityOne = 10 }
        };

        private const int MaxKillsPerObjective = 300;
        private const int WaitChecks = 20;

        public async Task Execute(IBotEngine instance)
        {
            if (instance == null) throw new ArgumentNullException(nameof(instance));
            BotData.BotState = BotData.State.Quest;
            Log("Swordhaven Rank 10 iniciado. Somente quests repetíveis 3065, 3066 e 3067.");

            await new CmdJoin2 { Map = "castle", Room = "1e99", Cell = "Enter", Pad = "Spawn" }.Execute(instance);
            if (!instance.IsRunning || !Player.IsLoggedIn) return;

            while (instance.IsRunning && Player.IsLoggedIn)
            {
                Faction faction = Player.Factions?.FirstOrDefault(f =>
                    f != null && string.Equals(f.Name, "Swordhaven", StringComparison.OrdinalIgnoreCase));
                int rank = faction?.Rank ?? 1;
                int totalRep = faction?.TotalRep ?? 0;

                Log("Swordhaven: Rank " + rank + " | reputação total " + totalRep + ".");
                if (rank >= 10)
                {
                    Log("Rank 10 alcançado. Bot encerrado.");
                    return;
                }

                bool completedAny = false;
                foreach (Step step in Steps)
                {
                    if (!instance.IsRunning || !Player.IsLoggedIn) return;

                    GameQuest quest = await LoadQuest(instance, step.QuestId);
                    if (quest == null)
                    {
                        Log("Não foi possível carregar a quest " + step.QuestId + ". Bot pausado para evitar loop.");
                        return;
                    }

                    if (!Player.Quests.IsInProgress(step.QuestId))
                    {
                        if (quest.IsMemberOnly || !quest.CanAccept)
                        {
                            Log("Quest " + step.QuestId + " indisponível no momento; pulando esta rodada.");
                            continue;
                        }

                        await instance.WaitUntil(() => World.IsActionAvailable(LockActions.AcceptQuest), null, 20, 500);
                        if (!instance.IsRunning) return;
                        Player.AcceptQuest(step.QuestId);
                        bool accepted = await WaitFor(instance, () => Player.Quests.IsInProgress(step.QuestId), WaitChecks);
                        if (!accepted)
                        {
                            Log("O jogo não confirmou a aceitação da quest " + step.QuestId + ". Encerrando.");
                            return;
                        }
                    }

                    Log("Executando: " + step.Title + ".");
                    bool ready = Player.Quests.CanComplete(step.QuestId);
                    int kills = 0;
                    while (instance.IsRunning && Player.IsLoggedIn && !ready && kills < MaxKillsPerObjective)
                    {
                        int before = GetRelevantProgress(step);
                        // Explicitly resolve and enter the target cell before invoking CmdKill.
                        // CmdKill's MoveToBotCell() otherwise returns to the profile's initial cell.
                        string targetCell = World.Monsters?
                            .Where(m => m != null && m.Alive && string.Equals(m.Name, step.Monster, StringComparison.OrdinalIgnoreCase)
                                && !string.IsNullOrWhiteSpace(m.Cell))
                            .Select(m => m.Cell).FirstOrDefault();
                        if (string.IsNullOrWhiteSpace(targetCell))
                        {
                            Log("Não encontrei célula ativa com " + step.Monster + " no mapa. Tentando novamente em 2 segundos.");
                            await Task.Delay(2000);
                            continue;
                        }
                        if (!string.Equals(Player.Cell, targetCell, StringComparison.OrdinalIgnoreCase))
                        {
                            Log("Movendo da célula " + Player.Cell + " para " + targetCell + " para enfrentar " + step.Monster + ".");
                            BotData.BotCell = targetCell;
                            BotData.BotPad = "Spawn";
                            Player.MoveToCell(targetCell, "Spawn");
                            await instance.WaitUntil(() => string.Equals(Player.Cell, targetCell, StringComparison.OrdinalIgnoreCase),
                                () => instance.IsRunning && Player.IsLoggedIn, 15, 500);
                        }
                        BotData.BotCell = targetCell;
                        BotData.BotPad = "Spawn";
                        if (!instance.IsRunning || !Player.IsLoggedIn) return;
                        var available = World.AvailableMonsters;
                        string names = available == null ? "(lista nula)" :
                            string.Join(", ", available.Where(m => m != null).Select(m => m.Name + "@" + m.Cell).Distinct().Take(30));
                        Log("Célula de combate " + Player.Cell + ". Alvos: " + names);
                        CmdKill kill = new CmdKill { Monster = step.Monster };
                        Log("Disparando CmdKill para " + step.Monster + ".");
                        await kill.ExecuteForKillFor(instance, () =>
                            Player.Quests.CanComplete(step.QuestId) || GetRelevantProgress(step) > before);
                        if (!instance.IsRunning || !Player.IsLoggedIn) return;
                        kills++;
                        ready = Player.Quests.CanComplete(step.QuestId);

                        // Item objectives are checked against inventory; kill-counter quests
                        // are checked against the live quest state from the game client.
                        if (!step.KillCounter && HasRequiredItems(step))
                            ready = await WaitFor(instance, () => Player.Quests.CanComplete(step.QuestId), 4);
                    }

                    if (!ready && !Player.Quests.CanComplete(step.QuestId))
                    {
                        Log("Sem confirmação de conclusão após " + kills + " eliminações em " + step.Title +
                            ". Encerrando para não consumir tempo em um loop infinito.");
                        return;
                    }

                    await instance.WaitUntil(() => World.IsActionAvailable(LockActions.TryQuestComplete), null, 20, 500);
                    if (!instance.IsRunning) return;
                    Player.CompleteQuest(step.QuestId);
                    bool turnedIn = await WaitFor(instance, () => !Player.Quests.IsInProgress(step.QuestId), WaitChecks);
                    if (!turnedIn)
                    {
                        Log("Entrega da quest " + step.QuestId + " não foi confirmada. Encerrando.");
                        return;
                    }
                    completedAny = true;
                    Log("Entregue: " + step.Title + ".");
                }

                Faction updated = Player.Factions?.FirstOrDefault(f =>
                    f != null && string.Equals(f.Name, "Swordhaven", StringComparison.OrdinalIgnoreCase));
                if ((updated?.Rank ?? 1) >= 10)
                {
                    Log("Rank 10 alcançado. Bot encerrado.");
                    return;
                }

                if (!completedAny)
                {
                    Log("Nenhuma quest repetível disponível nesta rodada. Bot pausado; verifique pré-requisitos e quests diárias.");
                    return;
                }
            }
        }

        private static async Task<GameQuest> LoadQuest(IBotEngine instance, int id)
        {
            GameQuest quest = Player.Quests.LoadedQuests?.FirstOrDefault(q => q.Id == id);
            if (quest != null) return quest;
            Player.Quests.GetQuest(id);
            await WaitFor(instance, () => Player.Quests.LoadedQuests != null &&
                Player.Quests.LoadedQuests.Any(q => q.Id == id), WaitChecks);
            return Player.Quests.LoadedQuests?.FirstOrDefault(q => q.Id == id);
        }

        private static bool HasRequiredItems(Step step)
        {
            return (string.IsNullOrEmpty(step.ItemOne) || HasItem(step.ItemOne, step.QuantityOne)) &&
                   (string.IsNullOrEmpty(step.ItemTwo) || HasItem(step.ItemTwo, step.QuantityTwo));
        }

        private static int GetRelevantProgress(Step step)
        {
            if (Player.Quests.CanComplete(step.QuestId)) return 1;
            return (string.IsNullOrEmpty(step.ItemOne) ? 0 : (HasItem(step.ItemOne, step.QuantityOne) ? step.QuantityOne : ItemQuantity(step.ItemOne))) +
                   (string.IsNullOrEmpty(step.ItemTwo) ? 0 : (HasItem(step.ItemTwo, step.QuantityTwo) ? step.QuantityTwo : ItemQuantity(step.ItemTwo)));
        }

        private static bool HasItem(string name, int quantity)
        {
            return Player.Inventory.ContainsItem(name, quantity.ToString()) ||
                   Player.TempInventory.ContainsItem(name, quantity.ToString());
        }

        private static int ItemQuantity(string name)
        {
            int inventoryQuantity = Player.Inventory.Items?.Where(i => i != null &&
                string.Equals(i.Name, name, StringComparison.OrdinalIgnoreCase)).Sum(i => i.Quantity) ?? 0;
            int temporaryQuantity = Player.TempInventory.Items?.Where(i => i != null &&
                string.Equals(i.Name, name, StringComparison.OrdinalIgnoreCase)).Sum(i => i.Quantity) ?? 0;
            return Math.Max(inventoryQuantity, temporaryQuantity);
        }

        private static async Task<bool> WaitFor(IBotEngine instance, Func<bool> condition, int checks)
        {
            for (int i = 0; i < checks && instance.IsRunning; i++)
            {
                if (condition()) return true;
                await Task.Delay(500);
            }
            return condition();
        }

        private static void Log(string message)
        {
            LogForm.Instance.AppendBot("[" + DateTime.Now.ToString("HH:mm:ss") +
                "] [Swordhaven] " + message + "\r\n");
        }

        public override string ToString()
        {
            return "Reputation: Swordhaven Rank 10";
        }
    }
}