//   ___|========================|___
//   \  |  Written by Felladrin  |  /	[IntelliSpawner] - Current version: 1.0.4 (December 17, 2015)
//    > |       June 2013        | <
//   /__|========================|__\	Based on RunUO Spawner.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Server.Commands;
using Server.Items;
using Server.Multis;
using CPA = Server.CommandPropertyAttribute;
using Server.Network;

/*
	UsesSpawnerHome true causes normal behavior, while false will
	cause the spawner to set the mobile's home to be its spawn 
	location, thus, not walking back to the spawner.  This will
	create a less artificial feel to mobiles attempting to return
	to their home location.

	Also, the spawn area and home range work together.  If the
	area is not set, they will behave pretty much like they 
	always have.  If the area is set, the mobile will spawn within
	that rectangle.  If both are set, the spawn location will be
	based on the rectangle and allow an additional # of tiles,
	which is the home range.

	Also, since the home does not necessarily equate to the spawner
	location any longer, a gettersetter was added to the 
	BaseCreature.
*/

namespace Server.Mobiles
{
    public class IntelliSpawner : Item, ISpawner
    {
        public static void Initialize()
        {
            new ActivationTimer().Start();
        }

        private int m_Team;
        private int m_HomeRange;
        private int m_WalkingRange;
        private int m_Count;
        private TimeSpan m_MinDelay;
        private TimeSpan m_MaxDelay;
        private List<string> m_SpawnNames;
        private List<ISpawnable> m_Spawned;
        private DateTime m_End;
        private InternalTimer m_Timer;
        private bool m_Running;
        private bool m_Group;
        private WayPoint m_WayPoint;
        private bool m_UsesSpawnerHome;
        private Rectangle2D m_SpawnArea;
        private bool m_IgnoreHousing;
        private bool m_MobilesSeekHome;

        public bool IsFull { get { return (m_Spawned.Count >= m_Count); } }
        public bool IsEmpty { get { return (m_Spawned.Count == 0); } }

        public List<string> SpawnNames
        {
            get { return m_SpawnNames; }
            set
            {
                m_SpawnNames = value;
                if (m_SpawnNames.Count < 1)
                    Stop();

                InvalidateProperties();
            }
        }

        public List<ISpawnable> Spawned
        {
            get { return m_Spawned; }
        }

        public virtual int SpawnNamesCount { get { return m_SpawnNames.Count; } }

        public override void OnAfterDuped(Item newItem)
        {
            IntelliSpawner s = newItem as IntelliSpawner;

            if (s == null)
                return;

            s.m_SpawnNames = new List<string>(m_SpawnNames);
            s.m_Spawned = new List<ISpawnable>();
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool IgnoreHousing
        {
            get
            {
                return m_IgnoreHousing;
            }
            set
            {
                m_IgnoreHousing = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool MobilesSeekHome
        {
            get
            {
                return m_MobilesSeekHome;
            }
            set
            {
                m_MobilesSeekHome = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Rectangle2D SpawnArea
        {
            get
            {
                return m_SpawnArea;
            }
            set
            {
                m_SpawnArea = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Count
        {
            get { return m_Count; }
            set { m_Count = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public WayPoint WayPoint
        {
            get
            {
                return m_WayPoint;
            }
            set
            {
                m_WayPoint = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Running
        {
            get { return m_Running; }
            set
            {
                if (value)
                    Start();
                else
                    Stop();

                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool UsesSpawnerHome
        {
            get
            {
                return m_UsesSpawnerHome;
            }
            set
            {
                m_UsesSpawnerHome = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int HomeRange
        {
            get { return m_HomeRange; }
            set { m_HomeRange = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int WalkingRange
        {
            get { return m_WalkingRange; }
            set { m_WalkingRange = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Team
        {
            get { return m_Team; }
            set { m_Team = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public TimeSpan MinDelay
        {
            get { return m_MinDelay; }
            set { m_MinDelay = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public TimeSpan MaxDelay
        {
            get { return m_MaxDelay; }
            set { m_MaxDelay = value; InvalidateProperties(); }
        }

        public DateTime End
        {
            get { return m_End; }
            set { m_End = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public TimeSpan NextSpawn
        {
            get
            {
                if (m_Running)
                    return m_End - DateTime.Now;
                else
                    return TimeSpan.FromSeconds(0);
            }
            set
            {
                Start();
                DoTimer(value);
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Group
        {
            get { return m_Group; }
            set { m_Group = value; InvalidateProperties(); }
        }

        [Constructable]
        public IntelliSpawner()
            : this(null)
        {
        }

        [Constructable]
        public IntelliSpawner(string spawnName)
            : this(1, 5, 10, 0, 4, spawnName)
        {
        }

        [Constructable]
        public IntelliSpawner(int amount, int minDelay, int maxDelay, int team, int homeRange, string spawnName)
            : this(amount, TimeSpan.FromMinutes(minDelay), TimeSpan.FromMinutes(maxDelay), team, homeRange, spawnName)
        {
        }

        [Constructable]
        public IntelliSpawner(int amount, TimeSpan minDelay, TimeSpan maxDelay, int team, int homeRange, string spawnName)
            : base(0x1f13)
        {
            List<string> spawnNames = new List<string>();

            if (!String.IsNullOrEmpty(spawnName))
                spawnNames.Add(spawnName);

            InitSpawner(amount, minDelay, maxDelay, team, homeRange, spawnNames);
        }

        public IntelliSpawner(int amount, TimeSpan minDelay, TimeSpan maxDelay, int team, int homeRange, List<string> spawnNames)
            : base(0x1f13)
        {
            InitSpawner(amount, minDelay, maxDelay, team, homeRange, spawnNames);
        }

        public override string DefaultName
        {
            get { return "IntelliSpawner"; }
        }

        private void InitSpawner(int amount, TimeSpan minDelay, TimeSpan maxDelay, int team, int homeRange, List<string> spawnNames)
        {
            Visible = false;
            Movable = false;
            m_Running = false;
            m_Group = false;
            m_MinDelay = minDelay;
            m_MaxDelay = maxDelay;
            m_Count = amount;
            m_Team = team;
            m_HomeRange = homeRange;
            m_WalkingRange = -1;
            m_SpawnNames = spawnNames;
            m_Spawned = new List<ISpawnable>();
        }

        public IntelliSpawner(Serial serial)
            : base(serial)
        {
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from.AccessLevel < AccessLevel.GameMaster)
                return;

            from.SendGump(new IntelliSpawnerGump(this));
            from.SendGump(new Gumps.PropertiesGump(from, this));
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            if (m_Running)
            {
                list.Add(1060742); // active

                list.Add(1060656, m_Count.ToString()); // amount to make: ~1_val~
                list.Add(1061169, m_HomeRange.ToString()); // range ~1_val~
                list.Add(1060658, "walking range\t{0}", m_WalkingRange); // ~1_val~: ~2_val~

                list.Add(1060659, "group\t{0}", m_Group); // ~1_val~: ~2_val~
                list.Add(1060660, "team\t{0}", m_Team); // ~1_val~: ~2_val~
                list.Add(1060661, "speed\t{0} to {1}", m_MinDelay, m_MaxDelay); // ~1_val~: ~2_val~

                if (m_SpawnNames.Count != 0)
                    list.Add(SpawnedStats());
            }
            else
            {
                list.Add(1060743); // inactive
            }
        }

        public override void OnSingleClick(Mobile from)
        {
            base.OnSingleClick(from);

            if (m_Running)
                LabelTo(from, "[Running]");
            else
                LabelTo(from, "[Off]");
        }

        public void Start()
        {
            if (!m_Running)
            {
                if (SpawnNamesCount > 0)
                {
                    m_Running = true;
                    DoTimer();
                }
            }
        }

        public void Stop()
        {
            if (m_Running)
            {
                if (m_Timer != null)
                    m_Timer.Stop();

                m_Running = false;
            }
        }

        public static string ParseType(string s)
        {
            return s.Split(null, 2)[0];
        }

        public void Defrag()
        {
            bool removed = false;

            for (int i = 0; i < m_Spawned.Count; ++i)
            {
                ISpawnable e = m_Spawned[i];

                bool toRemove = false;

                if (e is Item)
                {
                    Item item = (Item)e;

                    if (item.Deleted || item.Parent != null)
                        toRemove = true;
                }
                else if (e is Mobile)
                {
                    Mobile m = (Mobile)e;

                    if (m.Deleted)
                    {
                        toRemove = true;
                    }
                    else if (m is BaseCreature)
                    {
                        BaseCreature bc = (BaseCreature)m;

                        if (bc.Controlled || bc.IsStabled)
                        {
                            toRemove = true;
                        }
                    }
                }

                if (toRemove)
                {
                    m_Spawned.RemoveAt(i);
                    --i;
                    removed = true;
                }
            }

            if (removed)
                InvalidateProperties();
        }

        bool ISpawner.UnlinkOnTaming { get { return true; } }

        void ISpawner.Remove(ISpawnable spawn)
        {
            m_Spawned.Remove(spawn);

            InvalidateProperties();
        }

        public void OnTick()
        {
            DoTimer();

            bool stayActive = false;

            for (int i = 0; i < m_Spawned.Count; ++i)
            {
                ISpawnable spawned = m_Spawned[i];

                if (spawned is Mobile)
                {
                    Mobile creature = spawned as Mobile;

                    foreach (Mobile m in creature.GetMobilesInRange(Core.GlobalMaxUpdateRange))
                    {
                        if (ValidTrigger(m))
                        {
                            stayActive = true;
                            break;
                        }
                    }
                }
                else if (spawned is Item)
                {
                    Item item = spawned as Item;

                    foreach (Mobile m in item.GetMobilesInRange(Core.GlobalMaxUpdateRange))
                    {
                        if (ValidTrigger(m))
                        {
                            stayActive = true;
                            break;
                        }
                    }
                }

                if (stayActive)
                {
                    break;
                }
            }

            if (stayActive)
            {
                Defrag();

                if (IsEmpty)
                {
                    Respawn();
                }
                else
                {
                    Spawn();
                }
            }
            else
            {
                RemoveSpawned();
                m_Running = false;
            }
        }

        public override bool HandlesOnMovement { get { return true; } }

        public override void OnMovement(Mobile m, Point3D oldLocation)
        {
            if (!m_Running && ValidTrigger(m))
            {
                m_Running = true;
                Respawn();
                DoTimer();
            }
        }

        public virtual bool ValidTrigger(Mobile m)
        {
            if (m is BaseCreature)
            {
                BaseCreature bc = (BaseCreature)m;

                if (!bc.Controlled && !bc.Summoned)
                    return false;
            }
            else if (!m.Player)
            {
                return false;
            }

            return true;
        }

        public virtual void Respawn()
        {
            RemoveSpawned();

            for (int i = 0; i < m_Count; i++)
                Spawn();
        }

        public virtual void Spawn()
        {
            if (SpawnNamesCount > 0)
                Spawn(Utility.Random(SpawnNamesCount));
        }

        public void Spawn(string creatureName)
        {
            for (int i = 0; i < m_SpawnNames.Count; i++)
            {
                if (m_SpawnNames[i] == creatureName)
                {
                    Spawn(i);
                    break;
                }
            }
        }

        protected virtual ISpawnable CreateSpawnedObject(int index)
        {
            if (index >= m_SpawnNames.Count)
                return null;

            Type type = ScriptCompiler.FindTypeByName(ParseType(m_SpawnNames[index]));

            if (type != null)
            {
                try
                {
                    return Build(type, CommandSystem.Split(m_SpawnNames[index]));
                }
                catch
                {
                }
            }

            return null;
        }

        public static ISpawnable Build(Type type, string[] args)
        {
            bool isISpawnable = typeof(ISpawnable).IsAssignableFrom(type);

            if (!isISpawnable)
            {
                return null;
            }

            Add.FixArgs(ref args);

            string[,] props = null;

            for (int i = 0; i < args.Length; ++i)
            {
                if (Insensitive.Equals(args[i], "set"))
                {
                    int remains = args.Length - i - 1;

                    if (remains >= 2)
                    {
                        props = new string[remains / 2, 2];

                        remains /= 2;

                        for (int j = 0; j < remains; ++j)
                        {
                            props[j, 0] = args[i + (j * 2) + 1];
                            props[j, 1] = args[i + (j * 2) + 2];
                        }

                        Add.FixSetString(ref args, i);
                    }

                    break;
                }
            }

            PropertyInfo[] realProps = null;

            if (props != null)
            {
                realProps = new PropertyInfo[props.GetLength(0)];

                PropertyInfo[] allProps = type.GetProperties(BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public);

                for (int i = 0; i < realProps.Length; ++i)
                {
                    PropertyInfo thisProp = null;

                    string propName = props[i, 0];

                    for (int j = 0; thisProp == null && j < allProps.Length; ++j)
                    {
                        if (Insensitive.Equals(propName, allProps[j].Name))
                            thisProp = allProps[j];
                    }

                    if (thisProp != null)
                    {
                        CPA attr = Properties.GetCPA(thisProp);

                        if (attr != null && AccessLevel.GameMaster >= attr.WriteLevel && thisProp.CanWrite && !attr.ReadOnly)
                            realProps[i] = thisProp;
                    }
                }
            }

            ConstructorInfo[] ctors = type.GetConstructors();

            for (int i = 0; i < ctors.Length; ++i)
            {
                ConstructorInfo ctor = ctors[i];

                if (!Add.IsConstructable(ctor, AccessLevel.GameMaster))
                    continue;

                ParameterInfo[] paramList = ctor.GetParameters();

                if (args.Length == paramList.Length)
                {
                    object[] paramValues = Add.ParseValues(paramList, args);

                    if (paramValues == null)
                        continue;

                    object built = ctor.Invoke(paramValues);

                    if (built != null && realProps != null)
                    {
                        for (int j = 0; j < realProps.Length; ++j)
                        {
                            if (realProps[j] == null)
                                continue;

                            string result = Properties.InternalSetValue(built, realProps[j], props[j, 1]);
                        }
                    }

                    return (ISpawnable)built;
                }
            }

            return null;
        }

        public Point3D HomeLocation { get { return this.Location; } }

        public virtual bool CheckSpawnerFull()
        {
            return (m_Spawned.Count >= m_Count);
        }

        public void Spawn(int index)
        {
            Map map = Map;

            if (map == null || map == Map.Internal || SpawnNamesCount == 0 || index >= SpawnNamesCount || Parent != null)
                return;

            Defrag();

            if (CheckSpawnerFull())
                return;

            ISpawnable spawned = CreateSpawnedObject(index);

            if (spawned == null)
                return;

            spawned.Spawner = this;
            m_Spawned.Add(spawned);

            Point3D loc = (spawned is BaseVendor ? this.Location : GetSpawnPosition(spawned));

            spawned.OnBeforeSpawn(loc, map);
            spawned.MoveToWorld(loc, map);
            spawned.OnAfterSpawn();

            if (spawned is BaseCreature)
            {
                BaseCreature bc = (BaseCreature)spawned;

                if (m_WalkingRange >= 0)
                    bc.RangeHome = m_WalkingRange;
                else
                    bc.RangeHome = m_HomeRange;

                bc.CurrentWayPoint = m_WayPoint;

                bc.SeeksHome = m_MobilesSeekHome;

                if (m_Team > 0)
                    bc.Team = m_Team;

                bc.Home = (m_UsesSpawnerHome) ? this.HomeLocation : bc.Location;
            }

            InvalidateProperties();
        }

        public Point3D GetSpawnPosition()
        {
            return GetSpawnPosition(null);
        }

        private int GetAdjustedLocation(int range, int side, int coord, int coord_this)
        {
            return (((coord > 0) ? coord : (coord_this - range)) + (Utility.Random(Math.Max((((range * 2) + 1) + side), 1))));
        }

        public Point3D GetSpawnPosition(ISpawnable spawned)
        {
            Map map = Map;

            if (map == null)
                return Location;

            bool waterMob, waterOnlyMob;

            if (spawned is Mobile)
            {
                Mobile mob = (Mobile)spawned;

                waterMob = mob.CanSwim;
                waterOnlyMob = (mob.CanSwim && mob.CantWalk);
            }
            else
            {
                waterMob = false;
                waterOnlyMob = false;
            }

            for (int i = 0; i < 10; ++i)
            {
                int x = GetAdjustedLocation(m_HomeRange, m_SpawnArea.Width, m_SpawnArea.X, X);
                int y = GetAdjustedLocation(m_HomeRange, m_SpawnArea.Height, m_SpawnArea.Y, Y);

                int mapZ = map.GetAverageZ(x, y);

                if (m_IgnoreHousing || ((BaseHouse.FindHouseAt(new Point3D(x, y, mapZ), Map, 16) == null &&
                    BaseHouse.FindHouseAt(new Point3D(x, y, this.Z), Map, 16) == null)))
                {
                    if (waterMob)
                    {
                        if (IsValidWater(map, x, y, this.Z))
                            return new Point3D(x, y, this.Z);
                        else if (IsValidWater(map, x, y, mapZ))
                            return new Point3D(x, y, mapZ);
                    }

                    if (!waterOnlyMob)
                    {
                        if (map.CanSpawnMobile(x, y, this.Z))
                            return new Point3D(x, y, this.Z);
                        else if (map.CanSpawnMobile(x, y, mapZ))
                            return new Point3D(x, y, mapZ);
                    }
                }
            }

            return this.Location;
        }

        public static bool IsValidWater(Map map, int x, int y, int z)
        {
            if (!Region.Find(new Point3D(x, y, z), map).AllowSpawn() || !map.CanFit(x, y, z, 16, false, true, false))
                return false;

            LandTile landTile = map.Tiles.GetLandTile(x, y);

            if (landTile.Z == z && (TileData.LandTable[landTile.ID & TileData.MaxLandValue].Flags & TileFlag.Wet) != 0)
                return true;

            StaticTile[] staticTiles = map.Tiles.GetStaticTiles(x, y, true);

            for (int i = 0; i < staticTiles.Length; ++i)
            {
                StaticTile staticTile = staticTiles[i];

                if (staticTile.Z == z && (TileData.ItemTable[staticTile.ID & TileData.MaxItemValue].Flags & TileFlag.Wet) != 0)
                    return true;
            }

            return false;
        }

        public void DoTimer()
        {
            if (!m_Running)
                return;

            int minSeconds = (int)m_MinDelay.TotalSeconds;
            int maxSeconds = (int)m_MaxDelay.TotalSeconds;

            TimeSpan delay = TimeSpan.FromSeconds(Utility.RandomMinMax(minSeconds, maxSeconds));
            DoTimer(delay);
        }

        public virtual void DoTimer(TimeSpan delay)
        {
            if (!m_Running)
                return;

            m_End = DateTime.Now + delay;

            if (m_Timer != null)
                m_Timer.Stop();

            m_Timer = new InternalTimer(this, delay);
            m_Timer.Start();
        }

        private class ActivationTimer : Timer
        {
            public ActivationTimer() : base(TimeSpan.Zero, TimeSpan.FromSeconds(5))
            {
                Priority = TimerPriority.FiveSeconds;
            }

            protected override void OnTick()
            {
                List<Item> spawners = new List<Item>();

                foreach (NetState state in NetState.Instances)
                {
                    if (state.Mobile != null)
                    {
                        foreach (Item item in state.Mobile.GetItemsInRange(100))
                        {
                            if (item is IntelliSpawner)
                            {
                                spawners.Add(item);
                            }
                        }
                    }
                }

                foreach (Item item in spawners)
                {
                    IntelliSpawner spawner = item as IntelliSpawner;

                    if (!spawner.Running)
                    {
                        spawner.Running = true;
                        spawner.Respawn();
                        spawner.DoTimer();
                    }
                }
            }
        }

        private class InternalTimer : Timer
        {
            private IntelliSpawner m_Spawner;

            public InternalTimer(IntelliSpawner spawner, TimeSpan delay)
                : base(delay)
            {
                if (spawner.IsFull)
                    Priority = TimerPriority.FiveSeconds;
                else
                    Priority = TimerPriority.OneSecond;

                m_Spawner = spawner;
            }

            protected override void OnTick()
            {
                if (m_Spawner != null)
                    if (!m_Spawner.Deleted)
                        m_Spawner.OnTick();
            }
        }

        public string SpawnedStats()
        {
            Defrag();

            Dictionary<string, int> counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            foreach (string entry in m_SpawnNames)
            {
                string name = ParseType(entry);
                Type type = ScriptCompiler.FindTypeByName(name);

                if (type == null)
                    counts[name] = 0;
                else
                    counts[type.Name] = 0;
            }

            foreach (ISpawnable spawned in m_Spawned)
            {
                string name = spawned.GetType().Name;

                if (counts.ContainsKey(name))
                    ++counts[name];
                else
                    counts[name] = 1;
            }

            List<string> names = new List<string>(counts.Keys);
            names.Sort();

            StringBuilder result = new StringBuilder();

            for (int i = 0; i < names.Count; ++i)
                result.AppendFormat("{0}{1}: {2}", (i == 0) ? "" : "<BR>", names[i], counts[names[i]]);

            return result.ToString();
        }

        public int CountCreatures(string creatureName)
        {
            Defrag();

            int count = 0;

            for (int i = 0; i < m_Spawned.Count; ++i)
                if (Insensitive.Equals(creatureName, m_Spawned[i].GetType().Name))
                    ++count;

            return count;
        }

        public void RemoveSpawned(string creatureName)
        {
            Defrag();

            for (int i = m_Spawned.Count - 1; i >= 0; --i)
            {
                IEntity e = m_Spawned[i];

                if (Insensitive.Equals(creatureName, e.GetType().Name))
                    e.Delete();
            }

            InvalidateProperties();
        }

        public void RemoveSpawned()
        {
            Defrag();

            for (int i = m_Spawned.Count - 1; i >= 0; --i)
                m_Spawned[i].Delete();

            InvalidateProperties();
        }

        public void BringToHome()
        {
            Defrag();

            for (int i = 0; i < m_Spawned.Count; ++i)
            {
                ISpawnable e = m_Spawned[i];

                e.MoveToWorld(this.Location, this.Map);
            }
        }

        public override void OnDelete()
        {
            base.OnDelete();

            RemoveSpawned();

            if (m_Timer != null)
                m_Timer.Stop();
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)6); // version

            writer.Write(m_MobilesSeekHome);

            writer.Write(m_IgnoreHousing);

            writer.Write(m_SpawnArea);

            writer.Write(m_UsesSpawnerHome);

            writer.Write(m_WalkingRange);

            writer.Write(m_WayPoint);

            writer.Write(m_Group);

            writer.Write(m_MinDelay);
            writer.Write(m_MaxDelay);
            writer.Write(m_Count);
            writer.Write(m_Team);
            writer.Write(m_HomeRange);
            writer.Write(m_Running);

            if (m_Running)
                writer.WriteDeltaTime(m_End);

            writer.Write(m_SpawnNames.Count);

            for (int i = 0; i < m_SpawnNames.Count; ++i)
                writer.Write(m_SpawnNames[i]);

            writer.Write(m_Spawned.Count);

            for (int i = 0; i < m_Spawned.Count; ++i)
            {
                IEntity e = m_Spawned[i];

                if (e is Item)
                    writer.Write((Item)e);
                else if (e is Mobile)
                    writer.Write((Mobile)e);
                else
                    writer.Write(Serial.MinusOne);
            }
        }

        private static WarnTimer m_WarnTimer;

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 6:
                    {
                        m_MobilesSeekHome = reader.ReadBool();
                        m_UsesSpawnerHome = reader.ReadBool();
                        goto case 5;
                    }
                case 5:
                    {
                        m_SpawnArea = reader.ReadRect2D();
                        m_UsesSpawnerHome = reader.ReadBool();

                        goto case 4;
                    }
                case 4:
                    {
                        m_WalkingRange = reader.ReadInt();

                        goto case 3;
                    }
                case 3:
                case 2:
                    {
                        m_WayPoint = reader.ReadItem() as WayPoint;

                        goto case 1;
                    }

                case 1:
                    {
                        m_Group = reader.ReadBool();

                        goto case 0;
                    }

                case 0:
                    {
                        m_MinDelay = reader.ReadTimeSpan();
                        m_MaxDelay = reader.ReadTimeSpan();
                        m_Count = reader.ReadInt();
                        m_Team = reader.ReadInt();
                        m_HomeRange = reader.ReadInt();
                        m_Running = reader.ReadBool();

                        TimeSpan ts = TimeSpan.Zero;

                        if (m_Running)
                            ts = reader.ReadDeltaTime() - DateTime.Now;

                        int size = reader.ReadInt();

                        m_SpawnNames = new List<string>(size);

                        for (int i = 0; i < size; ++i)
                        {
                            string creatureString = reader.ReadString();

                            m_SpawnNames.Add(creatureString);
                            string typeName = ParseType(creatureString);

                            if (ScriptCompiler.FindTypeByName(typeName) == null)
                            {
                                if (m_WarnTimer == null)
                                    m_WarnTimer = new WarnTimer();

                                m_WarnTimer.Add(Location, Map, typeName);
                            }
                        }

                        int count = reader.ReadInt();

                        m_Spawned = new List<ISpawnable>(count);

                        for (int i = 0; i < count; ++i)
                        {
                            ISpawnable e = World.FindEntity(reader.ReadInt()) as ISpawnable;

                            if (e != null)
                            {
                                e.Spawner = this;
                                m_Spawned.Add(e);
                            }
                        }

                        if (m_Running)
                        {
                            RemoveSpawned();
                            m_Running = false;
                        }

                        break;
                    }
            }

            if (version < 3 && Weight == 0)
                Weight = -1;
        }

        private class WarnTimer : Timer
        {
            private List<WarnEntry> m_List;

            private class WarnEntry
            {
                public Point3D m_Point;
                public Map m_Map;
                public string m_Name;

                public WarnEntry(Point3D p, Map map, string name)
                {
                    m_Point = p;
                    m_Map = map;
                    m_Name = name;
                }
            }

            public WarnTimer()
                : base(TimeSpan.FromSeconds(1.0))
            {
                m_List = new List<WarnEntry>();
                Start();
            }

            public void Add(Point3D p, Map map, string name)
            {
                m_List.Add(new WarnEntry(p, map, name));
            }

            protected override void OnTick()
            {
                try
                {
                    Console.WriteLine("Warning: {0} bad spawns detected, logged: 'badspawn.log'", m_List.Count);

                    using (StreamWriter op = new StreamWriter("badspawn.log", true))
                    {
                        op.WriteLine("# Bad spawns : {0}", DateTime.Now);
                        op.WriteLine("# Format: X Y Z F Name");
                        op.WriteLine();

                        foreach (WarnEntry e in m_List)
                            op.WriteLine("{0}\t{1}\t{2}\t{3}\t{4}", e.m_Point.X, e.m_Point.Y, e.m_Point.Z, e.m_Map, e.m_Name);

                        op.WriteLine();
                        op.WriteLine();
                    }
                }
                catch
                {
                }
            }
        }
    }
}