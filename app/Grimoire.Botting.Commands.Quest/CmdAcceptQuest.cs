using Grimoire.UI;
using Grimoire.Game;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Grimoire.Botting.Commands.Quest
{
    public class CmdAcceptQuest : IBotCommand
    {
        public Game.Data.Quest Quest
        {
            get;
            set;
        }

        public async Task Execute(IBotEngine instance)
        {
            if (Quest == null)
                return;

            BotData.BotState = BotData.State.Quest;
            await instance.WaitUntil(() => World.IsActionAvailable(LockActions.AcceptQuest), null, 6, 500);

            if (Player.Quests.IsInProgress(Quest.Id))
            {
                LogForm.Instance.AppendBot($"[{DateTime.Now:HH:mm:ss}] Quest {Quest.Id} is already in progress; skipping acceptance and preserving it for the following bot commands.\r\n");
                return;
            }

            if (!Quest.CanAccept)
            {
                LogForm.Instance.AppendBot($"[{DateTime.Now:HH:mm:ss}] Quest {Quest.Id} is unavailable or member-only; acceptance skipped.\r\n");
                return;
            }

            Quest.Accept();
            await instance.WaitUntil(() => Player.Quests.IsInProgress(Quest.Id), null, 6, 500);
        }

        public override string ToString()
        {
            return $"Accept quest: {Quest.Id}";
        }
    }

    public class CmdAcceptQuest2 : IBotCommand
    {
        public string QuestID
        {
            get;
            set;
        }

        private int _questID => int.Parse(Bot.Instance.Value(QuestID));

        public async Task Execute(IBotEngine instance)
        {
            Game.Data.Quest quest = Player.Quests.LoadedQuests.FirstOrDefault(q => q.Id == _questID);
            if (quest == null)
            {
                Player.Quests.GetQuest(_questID);
                await instance.WaitUntil(() => Player.Quests.LoadedQuests.Any(q => q.Id == _questID), null, 10, 500);
                quest = Player.Quests.LoadedQuests.FirstOrDefault(q => q.Id == _questID);
            }

            if (quest == null)
            {
                LogForm.Instance.AppendBot($"[{DateTime.Now:HH:mm:ss}] Quest {_questID} could not be loaded; skipping acceptance.\r\n");
                return;
            }

            BotData.BotState = BotData.State.Quest;
            await instance.WaitUntil(() => World.IsActionAvailable(LockActions.AcceptQuest), null, 10, 500);

            if (Player.Quests.IsInProgress(_questID))
            {
                LogForm.Instance.AppendBot($"[{DateTime.Now:HH:mm:ss}] Quest {_questID} is already in progress; skipping acceptance.\r\n");
                return;
            }

            if (!quest.CanAccept)
            {
                LogForm.Instance.AppendBot($"[{DateTime.Now:HH:mm:ss}] Quest {_questID} is unavailable or member-only; acceptance skipped.\r\n");
                return;
            }

            Player.AcceptQuest(_questID);
            await instance.WaitUntil(() => Player.Quests.IsInProgress(_questID), null, 6, 500);
        }

        public override string ToString()
        {
            return $"Accept quest: {QuestID}";
        }
    }

}
