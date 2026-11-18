using Grimoire.Game.Data;
using Grimoire.Tools;
using Grimoire.Botting;
using Grimoire.UI;
using Grimoire.Utils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace Grimoire.Game
{
    public static class World
    {

        public static List<Map> Maps
        {
            get { return GameSessionState.Current.Maps; }
            set { GameSessionState.Current.Maps = value; }
        }

        public static Map CurrentMap
        {
            get { return GameSessionState.Current.CurrentMap; }
            set { GameSessionState.Current.CurrentMap = value; }
        }

        public static List<ShopInfo> Shops
        {
            get { return GameSessionState.Current.Shops; }
            set { GameSessionState.Current.Shops = value; }
        }

        public static ShopInfo CurrentShop
        {
            get { return GameSessionState.Current.CurrentShop; }
            set { GameSessionState.Current.CurrentShop = value; }
        }

        public static List<InventoryItem> Drops
        {
            get { return GameSessionState.Current.Drops; }
            set { GameSessionState.Current.Drops = value; }
        }

        public static Server CurrentServer
        {
            get { return GameSessionState.Current.CurrentServer; }
            set { GameSessionState.Current.CurrentServer = value; }
        }

        public static List<Quest> Quests
        {
            get { return GameSessionState.Current.WorldQuests; }
            set { GameSessionState.Current.WorldQuests = value; }
        }

        public static void RefreshDictionary() => _players = JsonConvert.DeserializeObject<Dictionary<string, PlayerInfo>>(Flash.Call("Players", new object[0]));

        public static Dictionary<string, PlayerInfo> _players
        {
            get { return GameSessionState.Current.Players; }
            set { GameSessionState.Current.Players = value; }
        }
        /// <summary>
        /// Gets a list of all players in the current map.
        /// </summary>
        public static List<PlayerInfo> Players => _players.Values.ToList();

        public static List<ShopInfo> LoadedShops
        {
            get { return GameSessionState.Current.LoadedShops; }
            set { GameSessionState.Current.LoadedShops = value; }
        }

        public static DropStack DropStack => GameSessionState.Current.DropStack;

        private static readonly Dictionary<LockActions, string> LockedActions;

        public static List<Monster> VisibleMonsters => Flash.Call<List<Monster>>("GetVisibleMonstersInCell", new string[0]);
        //public static List<Monster> VisibleMonsters => CurrentMap.Monsters.FindAll(mon => mon.Cell.Equals(Player.Cell) && mon.Alive);

        public static List<Monster> AvailableMonsters => Flash.Call<List<Monster>>("GetMonstersInCell", new string[0]);
        //public static List<Monster> AvailableMonsters => CurrentMap.Monsters.FindAll(mon => mon.Cell.Equals(Player.Cell));

        public static bool IsMapLoading => !Flash.Call<bool>("MapLoadComplete", new string[0]);

        public static List<string> PlayersInMap => Flash.Call<List<string>>("PlayersInMap", new string[0]);
        //public static List<string> PlayersInMap => Map.Players.ConvertAll<string>((Avatar a) => a.Username);

        public static List<InventoryItem> ItemTree => Flash.Call<List<InventoryItem>>("GetItemTree", new string[0]);

        public static string[] Cells => Flash.Call<string[]>("GetCells", new string[0]);

        public static List<string> Pads => Flash.Call<List<string>>("GetPads", new string[0]);

        //public static int RoomId => Flash.Call<int>("RoomId", new string[0]);
        public static int RoomId => Map.ID;

        //public static int RoomNumber => Flash.Call<int>("RoomNumber", new string[0]);
        public static int RoomNumber => int.Parse(Map.Instance);

        public static event Action<InventoryItem> ItemDropped
        {
            add { GameSessionState.Current.ItemDropped += value; }
            remove { GameSessionState.Current.ItemDropped -= value; }
        }

        public static event Action<ShopInfo> ShopLoaded
        {
            add { GameSessionState.Current.ShopLoaded += value; }
            remove { GameSessionState.Current.ShopLoaded -= value; }
        }

        public static void OnItemDropped(InventoryItem drop)
        {
            Action<InventoryItem> itemDropped = GameSessionState.Current.ItemDropped;
            itemDropped?.Invoke(drop);
            if (Drops.Find(d => d.Id == drop.Id) == null)
            {
                Drops.Add(drop);
            }
            InventoryItem item = Player.Bank.SavedItems.FirstOrDefault((InventoryItem it) => it.Name.Equals(drop.Name, StringComparison.OrdinalIgnoreCase));
            int dropQty = (item != null) ? item.Quantity : (Player.Inventory.Items.Find((InventoryItem x) => x.Name.Equals(drop.Name)) ?? new InventoryItem()).Quantity;
            LogForm.Instance.AppendDrops($"[Item Drop] ({dropQty}) {drop.Name} x {drop.Quantity} at {DateTime.Now:hh:mm:ss tt}.\r\n");
        }

        public static void OnShopLoaded(ShopInfo shopInfo)
        {
            GameSessionState.Current.ShopLoaded?.Invoke(shopInfo);
            LoadedShops.Add(shopInfo);
        }

        public static bool IsActionAvailable(LockActions action) => Flash.Call<bool>("IsActionAvailable", LockedActions[action]);

        public static void SetSpawnPoint() => Flash.Call("SetSpawnPoint", new string[0]);

        public static bool IsMonsterAvailable(string name) => Flash.Call<bool>("IsMonsterAvailable", new string[1] { name });
        //public static bool IsMonsterAvailable(string name) => CurrentMap.Monsters.Find(mon => mon.Name.Equals(name, StringComparison.OrdinalIgnoreCase) && mon.Alive) != null;

        public static int MonsterHealth(string name) => Flash.Call<int>("MonsterHealth", new string[1] { name });
        //public static int MonsterHealth(string name) => CurrentMap.Monsters.Find(mon => mon.Name.Equals(name, StringComparison.OrdinalIgnoreCase)).Health;

        public static void ReloadCurrentMap() => Flash.Call("reloadCurrentMap", new string[0]);

        public static void GameMessage(string msg) => Flash.Call("GameMessage", msg);

        public static string ServerTime() => Flash.Call<string>("GetServerTime", new string[0]);

        public static string ServerName() => Flash.Call<string>("GetServerName", new string[0]);
        //public static string ServerName() => CurrentServer.Name;

        public static void SendClientPacket(string packet, string type = "str")
        {
            Flash.Call("sendClientPacket", packet, type);
        }

        public static void SendPacket(string packet, string type = "String")
        {
            Flash.CallGameFunction("sfc.send" + type, packet);
        }

        public static List<Avatar> AvatarsInMap => Flash.Call<List<Avatar>>("AvatarsInMap", new string[0]);
        //public static List<Avatar> AvatarsInMap => Map.Players;

        public static List<Avatar> AvatarsInMyCell => AvatarsInMap.FindAll(a => a.Cell == Player.Cell);

        public static List<Avatar> AvatarsInCell(string cell)
        {
            string targetedCell = (cell != "Wait" && cell != "Blank") ? cell : "Enter";
            return AvatarsInMap.FindAll(a => a.Cell == targetedCell);
        }

        public static List<Monster> MonstersInMap => Flash.Call<List<Monster>>("MonstersInMap", new string[0]);

        /// <summary>
        /// Gets clickable map items exposed by the current cell's mapItem components.
        /// The ActionScript GetMapItems callback returns ID, name, quest ID, item name and collect message.
        /// </summary>
        public static List<MapItem> MapItems => Flash.Call<List<MapItem>>("GetMapItems", new string[0]) ?? new List<MapItem>();

        public static List<Monster> Monsters =>
            Flash.Call<List<Monster>>("selectArrayObjects", "world.monsters", "objData") ?? new List<Monster>();

        //public static List<Monster> MonstersInCell(string cell) => Monsters.FindAll(m => m.Cell == cell);
        public static List<Monster> MonstersInCell(string cell) => Monsters.FindAll(mon => mon.Cell == cell);

        public static List<Monster> VisibleMonster(string cell) => Monsters.FindAll(m => (m.Cell == cell) && m.Alive);
        //public static List<Monster> VisibleMonster(string cell) => CurrentMap.Monsters.FindAll(mon => mon.Cell == cell && mon.Alive);

        public static bool IsGameLoaded() => Flash.Call<bool>("InGame", new string[0]);

        public static bool VisibleDropUI => Flash.Call<bool>("IsDropMenuOpen", new string[0]);

        public static bool VisibleConnScreen => Flash.Call<bool>("InConnStage", new string[0]);

        public static bool uiLock => Flash.Instance.GetGameObject<bool>("world.uiLock");

        public static bool VisibleWorld => Flash.Instance.GetGameObject<bool>("world.visible");

        public static int ActiveCutscene => Flash.Instance.GetGameObject<int>("mcExtSWF.numChildren");

        public static void GamePopup(string text, bool red = false) => Flash.Call("GamePopup", text, red);

        public static List<Aura> PlayerAuras
        {
            get { return GameSessionState.Current.PlayerAuras; }
            set { GameSessionState.Current.PlayerAuras = value; }
        }

        public static List<Aura> EnemyAuras
        {
            get { return GameSessionState.Current.EnemyAuras; }
            set { GameSessionState.Current.EnemyAuras = value; }
        }

        internal static object AuraSyncRoot => GameSessionState.Current.AuraSyncRoot;

        public static event Action<Aura> auraCountdown
        {
            add { GameSessionState.Current.AuraCountdown += value; }
            remove { GameSessionState.Current.AuraCountdown -= value; }
        }

        internal static void OnAuraExpired(Aura aura) => GameSessionState.Current.AuraCountdown?.Invoke(aura);

        public static bool FPSBox
        {
            get => Flash.Instance.GetGameObject<bool>("ui.mcFPS.visible");
            set => Flash.Instance.SetGameObject("ui.mcFPS.visible", value);
        }

        public static List<Cell> GetMapCells => Flash.Call<List<Cell>>("GetMapCells", new string[0]);

        public static bool mapLoadInProgress => Flash.Instance.GetGameObject<bool>("world.mapLoadInProgress");

        public static Cell CellInfo(string cell) => Flash.Call<Cell>("GetCellInfo", cell);

        /// <summary>
        /// Scans every named cell and returns map items together with the cell where each was found.
        /// The cell is required because map-item IDs may only be actionable after entering that cell.
        /// </summary>
        public static List<MapItemLocation> MapItemLocationsInMap
        {
            get
            {
                var result = new List<MapItemLocation>();
                var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (string cellName in Cells ?? Array.Empty<string>())
                {
                    if (string.IsNullOrWhiteSpace(cellName)
                        || cellName.Equals("Wait", StringComparison.OrdinalIgnoreCase)
                        || cellName.Equals("Blank", StringComparison.OrdinalIgnoreCase))
                        continue;

                    Cell cell = CellInfo(cellName);
                    if (cell?.MapItems == null)
                        continue;

                    foreach (MapItem item in cell.MapItems)
                    {
                        if (item == null || item.ID <= 0)
                            continue;

                        // Include the cell in the key: IDs are not guaranteed to be globally unique.
                        string key = cellName + ":" + item.ID;
                        if (seen.Add(key))
                            result.Add(new MapItemLocation { CellName = cellName, Item = item });
                    }
                }
                return result;
            }
        }

        /// <summary>
        /// Backwards-compatible flattened view of all map items.
        /// </summary>
        public static List<MapItem> MapItemsInMap
        {
            get { return MapItemLocationsInMap.Select(location => location.Item).ToList(); }
        }

        /// <summary>
        /// Returns map items tagged for the specified quest, preserving their cell locations.
        /// </summary>
        public static List<MapItemLocation> MapItemLocationsForQuest(int questId)
        {
            return MapItemLocationsInMap
                .Where(location => location.Item.QuestID == questId)
                .ToList();
        }

        /// <summary>
        /// Returns only clickable map items tagged for the specified quest.
        /// </summary>
        public static List<MapItem> MapItemsForQuest(int questId)
        {
            return MapItemLocationsForQuest(questId)
                .Select(location => location.Item)
                .ToList();
        }

        public static List<Cell> AllCellInfo => Flash.Call<List<Cell>>("GetAllPads", true);

        public static void ScanMap() => Flash.Call("GetAllPads", false);

        static World()
        {
            LockedActions = new Dictionary<LockActions, string>(14)
            {
                {
                    LockActions.LoadShop,
                    "loadShop"
                },
                {
                    LockActions.LoadEnhShop,
                    "loadEnhShop"
                },
                {
                    LockActions.LoadHairShop,
                    "loadHairShop"
                },
                {
                    LockActions.EquipItem,
                    "equipItem"
                },
                {
                    LockActions.UnequipItem,
                    "unequipItem"
                },
                {
                    LockActions.BuyItem,
                    "buyItem"
                },
                {
                    LockActions.SellItem,
                    "sellItem"
                },
                {
                    LockActions.GetMapItem,
                    "getMapItem"
                },
                {
                    LockActions.TryQuestComplete,
                    "tryQuestComplete"
                },
                {
                    LockActions.AcceptQuest,
                    "acceptQuest"
                },
                {
                    LockActions.DoIA,
                    "doIA"
                },
                {
                    LockActions.Rest,
                    "rest"
                },
                {
                    LockActions.Who,
                    "who"
                },
                {
                    LockActions.Transfer,
                    "tfer"
                }
            };
        }
    }
    /// <summary>
    /// Performs bounded, quest-scoped collection of clickable map items.
    /// Callers must supply an item predicate and a live completion check so the
    /// collector never clicks unrelated objects or assumes a click succeeded.
    /// </summary>
    public static class MapItemCollector
    {
        public static async Task<int> CollectForQuest(
            int questId,
            Func<MapItem, bool> isRequiredItem,
            Func<bool> objectiveComplete,
            Func<bool> keepRunning,
            int maxCellVisits = 12,
            int maxItemAttempts = 40)
        {
            if (questId <= 0)
                throw new ArgumentOutOfRangeException("questId");
            if (isRequiredItem == null)
                throw new ArgumentNullException("isRequiredItem");
            if (objectiveComplete == null)
                throw new ArgumentNullException("objectiveComplete");
            if (keepRunning == null)
                throw new ArgumentNullException("keepRunning");

            int collectedAttempts = 0;
            var visitedCells = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            string originalCell = Player.Cell;
            string originalPad = Player.Pad;

            while (keepRunning()
                && !objectiveComplete()
                && visitedCells.Count < maxCellVisits
                && collectedAttempts < maxItemAttempts)
            {
                List<MapItemLocation> candidates = World.MapItemLocationsForQuest(questId)
                    .Where(location => location != null
                        && location.Item != null
                        && !string.IsNullOrWhiteSpace(location.CellName)
                        && isRequiredItem(location.Item)
                        && !visitedCells.Contains(location.CellName))
                    .ToList();

                if (candidates.Count == 0)
                    break;

                string destinationCell = candidates[0].CellName;
                visitedCells.Add(destinationCell);

                if (!string.Equals(Player.Cell, destinationCell, StringComparison.OrdinalIgnoreCase))
                {
                    Cell cellInfo = World.CellInfo(destinationCell);
                    string pad = cellInfo?.Pads?.FirstOrDefault();
                    if (string.IsNullOrWhiteSpace(pad))
                        pad = "Spawn";

                    Player.MoveToCell(destinationCell, pad);
                    int waitCount = 0;
                    while (keepRunning()
                        && !string.Equals(Player.Cell, destinationCell, StringComparison.OrdinalIgnoreCase)
                        && waitCount++ < 30)
                    {
                        await Task.Delay(250);
                    }
                }

                if (!keepRunning() || objectiveComplete())
                    break;
                if (!string.Equals(Player.Cell, destinationCell, StringComparison.OrdinalIgnoreCase))
                    continue;

                // A cell may contain several required objects (for example, multiple
                // clickable quest creatures). Re-read after each click and avoid
                // clicking the same live object repeatedly.
                var clickedIds = new HashSet<int>();
                int cellAttempts = 0;
                while (keepRunning()
                    && !objectiveComplete()
                    && collectedAttempts < maxItemAttempts
                    && cellAttempts++ < 20)
                {
                    MapItem liveTarget = World.MapItems.FirstOrDefault(item =>
                        item != null
                        && item.ID > 0
                        && item.QuestID == questId
                        && !clickedIds.Contains(item.ID)
                        && isRequiredItem(item));

                    if (liveTarget == null)
                        break;

                    int actionWait = 0;
                    while (keepRunning()
                        && !World.IsActionAvailable(LockActions.GetMapItem)
                        && actionWait++ < 20)
                    {
                        await Task.Delay(250);
                    }

                    if (!keepRunning() || objectiveComplete())
                        break;
                    if (!World.IsActionAvailable(LockActions.GetMapItem))
                        break;

                    clickedIds.Add(liveTarget.ID);
                    Player.GetMapItem(liveTarget.ID);
                    collectedAttempts++;

                    int verifyWait = 0;
                    while (keepRunning()
                        && !objectiveComplete()
                        && verifyWait++ < 12)
                    {
                        await Task.Delay(250);
                    }
                }
            }

            if (keepRunning()
                && !objectiveComplete()
                && !string.IsNullOrWhiteSpace(originalCell)
                && !string.Equals(Player.Cell, originalCell, StringComparison.OrdinalIgnoreCase))
            {
                Player.MoveToCell(originalCell, originalPad);
            }

            return collectedAttempts;
        }
    }

}
