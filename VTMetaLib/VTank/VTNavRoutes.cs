using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VTMetaLib.VTank
{
    public enum NavRouteType
    {
        Circular = 1,
        Linear = 2,
        Follow = 3,
        Once = 4
    }

    public enum NavNodeType
    {
        Point = 0,
        Portal = 1, // deprecated
        Recall = 2,
        Pause = 3,
        Chat = 4,
        OpenVendor = 5,
        UseObject = 6, // Portal/NPC
        TalkToNPC = 7,
        Checkpoint = 8,
        Jump = 9,
        Other = 99, // unsupported

    }
    public enum RecallSpell
    {
        Primary = 48,
        Secondary = 2647,
        Lifestone = 1635,
        LifestoneSending = 1636,
        Portal = 2645,
        Aphus = 2931,
        Sanctuary = 2023,
        Caul = 2943,
        GW = 3865,
        Aerlinthe = 2041,
        Ulgrim = 2941,
        Bur = 4084,
        ParadoxOlthoi = 4198,
        Graveyard = 4128,
        Colosseum = 4213,
        FacilityHub = 5175,
        GearKnightCamp = 5330,
        Neftet = 5541,
        Candeth = 4214,
        Rynthid = 6150,
        ViridianRise = 6321,
        ViridianTree = 6322,
        SocietyCH = 6325,
        SocietyRB = 6327,
        SocietyEW = 6326
    }

    public abstract class VTNavRouteEntityBase : VTEncodableWithSerializer
    {
        public int TypeId { get; private set; }

        protected VTNavRouteEntityBase(int typeId)
        {
            TypeId = typeId;
        }

        public void ReadDataFrom(MetaFile file)
        {
            ReadFrom(file);
        }

        public abstract void ReadFrom(MetaFile file);

        public abstract void WriteTo(MetaFileBuilder writer);

        public void ReadFromData(MetaFile file, VTDataType data)
        {
            throw new NotImplementedException("Unable to read Nav Route models to VTData primitives");
        }

        public VTDataType AsVTData()
        {
            throw new NotImplementedException("Unable to convert Nav Route models to VTData primitives");
        }
    }

    public abstract class VTNavRoute : VTNavRouteEntityBase
    {
        public string RouteName { get; set; } = "EmbeddedNav";

        public NavRouteType RouteType { get; private set; }

        protected VTNavRoute(NavRouteType routeType, string name = "") : base((int)routeType)
        {
            RouteType = routeType;
            if (!string.IsNullOrWhiteSpace(name))
                RouteName = name;
        }
    }

    public class NavListRoute : VTNavRoute
    {
        public List<VTNavNode> Nodes { get; } = new List<VTNavNode>();

        public int NodeCount => Nodes.Count;

        public NavListRoute(NavRouteType routeType, string name = "", List<VTNavNode> initialNodes = null) : base(routeType, name)
        {
            if (initialNodes != null)
                Nodes.AddRange(initialNodes);
        }

        public override void ReadFrom(MetaFile file)
        {
            int nodeCount = file.ReadNextLineAsInt();
            for (int i = 0; i < nodeCount; i++)
            {
                int nodeTypeId = file.ReadNextLineAsInt();
                NavNodeType nodeType = (NavNodeType)nodeTypeId;
                VTNavNode node = nodeType.NewNode();
                node.ReadFrom(file);
                Nodes.Add(node);
            }
        }

        public override void WriteTo(MetaFileBuilder writer)
        {
            writer.WriteLine(NodeCount.ToString());
            foreach (var node in Nodes)
                node.WriteTo(writer);
        }
    }

    public class NavFollowRoute : VTNavRoute
    {
        public string TargetName { get; private set; }

        public int TargetWID { get; private set; }

        public NavFollowRoute() : base(NavRouteType.Follow) { }

        public NavFollowRoute(string targetName, int targetWID) : this()
        {
            TargetName = targetName;
            TargetWID = targetWID;
        }

        public override void ReadFrom(MetaFile file)
        {
            TargetName = file.ReadNextLineAsString();
            TargetWID = file.ReadNextLineAsInt();
        }

        public override void WriteTo(MetaFileBuilder writer)
        {
            writer.WriteLine(TargetName);
            writer.WriteLine(TargetWID.ToString());
        }
    }

    public abstract class VTNavNode : VTNavRouteEntityBase
    {
        public NavNodeType NodeType { get; private set; }

        protected VTNavNode(NavNodeType nodeType) : base((int)nodeType)
        {
            NodeType = nodeType;
        }
    }


    public abstract class NavNodeWithCoords : VTNavNode
    {
        public double X { get; private set; }
        public double Y { get; private set; }
        public double Z { get; private set; }

        protected NavNodeWithCoords(NavNodeType nodeType) : base(nodeType) { }

        protected NavNodeWithCoords(NavNodeType nodeType, double x, double y, double z) : base(nodeType)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public override void ReadFrom(MetaFile file)
        {
            X = file.ReadNextLineAsDouble();
            Y = file.ReadNextLineAsDouble();
            Z = file.ReadNextLineAsDouble();
            int filler = file.ReadNextLineAsInt();
            // TODO assert == zero?
            ReadNodeData(file);
        }

        protected abstract void ReadNodeData(MetaFile file);

        public override void WriteTo(MetaFileBuilder writer)
        {
            writer.WriteLine(X.ToString());
            writer.WriteLine(Y.ToString());
            writer.WriteLine(Z.ToString());
            writer.WriteLine("0");
            WriteNodeData(writer);
        }

        protected abstract void WriteNodeData(MetaFileBuilder writer);
    }

    public class NavNodePoint : NavNodeWithCoords
    {
        public NavNodePoint() : base(NavNodeType.Point) { }

        public NavNodePoint(double x, double y, double z) : base(NavNodeType.Point, x, y, z) { }

        protected override void ReadNodeData(MetaFile file) { }

        protected override void WriteNodeData(MetaFileBuilder writer) { }
    }

    public class NavNodePortal : VTNavNode
    {
        public int PortalWID { get; private set; }

        public NavNodePortal() : base(NavNodeType.Portal) { }

        public NavNodePortal(int portalWID) : this()
        {
            PortalWID = portalWID;
        }

        public override void ReadFrom(MetaFile file)
        {
            throw new NotImplementedException("Portal nav nodes are deprecated");
        }

        public override void WriteTo(MetaFileBuilder writer)
        {
            throw new NotImplementedException("Portal nav nodes are deprecated");
        }
    }

    public class NavNodeRecall : NavNodeWithCoords
    {

        public int SpellId { get; private set; }

        public NavNodeRecall() : base(NavNodeType.Recall) { }

        public NavNodeRecall(double x, double y, double z, int spellId) : base(NavNodeType.Recall, x, y, z) 
        {
            SpellId = spellId;
        }

        protected override void ReadNodeData(MetaFile file)
        {
            SpellId = file.ReadNextLineAsInt();
        }

        protected override void WriteNodeData(MetaFileBuilder writer)
        {
            writer.WriteLine(SpellId.ToString());
        }
    }

    public class NavNodePause : NavNodeWithCoords
    {
        public double Seconds { get; private set; }

        public NavNodePause() : base(NavNodeType.Pause) { }

        public NavNodePause(double seconds) : this()
        {
            Seconds = seconds;
        }

        protected override void ReadNodeData(MetaFile file)
        {
            Seconds = file.ReadNextLineAsDouble();
        }

        protected override void WriteNodeData(MetaFileBuilder writer)
        {
            writer.WriteLine(Seconds.ToString());
        }
    }

    public class NavNodeChatCommand : NavNodeWithCoords
    {
        public string ChatText { get; private set; }

        public NavNodeChatCommand() : base(NavNodeType.Chat) { }

        public NavNodeChatCommand(string text) : this()
        {
            ChatText = text;
        }

        protected override void ReadNodeData(MetaFile file)
        {
            ChatText = file.ReadNextLineAsString();
        }

        protected override void WriteNodeData(MetaFileBuilder writer)
        {
            writer.WriteLine(ChatText);
        }
    }

    public class NavNodeOpenVendor : NavNodeWithCoords
    {
        public int VendorWID { get; private set; }

        public string VendorName { get; private set; }

        public string ChatText { get; private set; }

        public NavNodeOpenVendor() : base(NavNodeType.OpenVendor) { }

        public NavNodeOpenVendor(int vendorWID, string vendorName) : this()
        {
            VendorWID = vendorWID;
            VendorName = vendorName;
        }

        protected override void ReadNodeData(MetaFile file)
        {
            VendorWID = file.ReadNextLineAsInt();
            VendorName = file.ReadNextLineAsString();
        }

        protected override void WriteNodeData(MetaFileBuilder writer)
        {
            writer.WriteLine(VendorWID.ToString());
            writer.WriteLine(VendorName);
        }
    }

    public class NavNodeUseObject : NavNodeWithCoords
    {
        public string ObjectName { get; private set; }
        public int ObjectClass { get; private set; }

        public double ObjectX { get; private set; }
        public double ObjectY { get; private set; }
        public double ObjectZ { get; private set; }

        public NavNodeUseObject() : base(NavNodeType.UseObject) { }

        public NavNodeUseObject(double x, double y, double z, string objName, int objClass, double objX, double objY, double objZ) : base (NavNodeType.UseObject, x, y, z)
        {
            ObjectName = objName;
            ObjectClass = objClass;
            ObjectX = objX;
            ObjectY = objY;
            ObjectZ = objZ;
        }

        protected override void ReadNodeData(MetaFile file)
        {
            ObjectName = file.ReadNextLineAsString();
            ObjectClass = file.ReadNextLineAsInt();
            bool filler = file.ReadNextLineAsBoolean();
            // TODO assert == true ?
            ObjectX = file.ReadNextLineAsDouble();
            ObjectY = file.ReadNextLineAsDouble();
            ObjectZ = file.ReadNextLineAsDouble();
        }

        protected override void WriteNodeData(MetaFileBuilder writer)
        {
            writer.WriteLine(ObjectName);
            writer.WriteLine(ObjectClass.ToString());
            writer.WriteLine("True");
            writer.WriteLine(ObjectX.ToString());
            writer.WriteLine(ObjectY.ToString());
            writer.WriteLine(ObjectZ.ToString());
        }
    }

    public class NavNodeTalkToNPC : NavNodeWithCoords
    {
        public string ObjectName { get; private set; }
        public int ObjectClass { get; private set; }

        public double ObjectX { get; private set; }
        public double ObjectY { get; private set; }
        public double ObjectZ { get; private set; }

        public NavNodeTalkToNPC() : base(NavNodeType.TalkToNPC) { }

        public NavNodeTalkToNPC(double x, double y, double z, string objName, int objClass, double objX, double objY, double objZ) : base(NavNodeType.TalkToNPC, x, y, z)
        {
            ObjectName = objName;
            ObjectClass = objClass;
            ObjectX = objX;
            ObjectY = objY;
            ObjectZ = objZ;
        }

        protected override void ReadNodeData(MetaFile file)
        {
            ObjectName = file.ReadNextLineAsString();
            ObjectClass = file.ReadNextLineAsInt();
            bool filler = file.ReadNextLineAsBoolean();
            // TODO assert == true ?
            ObjectX = file.ReadNextLineAsDouble();
            ObjectY = file.ReadNextLineAsDouble();
            ObjectZ = file.ReadNextLineAsDouble();
        }

        protected override void WriteNodeData(MetaFileBuilder writer)
        {
            writer.WriteLine(ObjectName);
            writer.WriteLine(ObjectClass.ToString());
            writer.WriteLine("True");
            writer.WriteLine(ObjectX.ToString());
            writer.WriteLine(ObjectY.ToString());
            writer.WriteLine(ObjectZ.ToString());
        }
    }

    public class NavNodeCheckpoint : NavNodeWithCoords
    {
        public NavNodeCheckpoint() : base(NavNodeType.Checkpoint) { }

        public NavNodeCheckpoint(double x, double y, double z) : base(NavNodeType.Checkpoint, x, y, z) { }

        protected override void ReadNodeData(MetaFile file) { }

        protected override void WriteNodeData(MetaFileBuilder writer) { }
    }

    public class NavNodeJump : NavNodeWithCoords
    {
        public double Heading { get; private set; }

        public bool HoldShift { get; private set; }

        public double DurationSecs { get; private set; }

        public NavNodeJump() : base(NavNodeType.Jump) { }

        public NavNodeJump(double x, double y, double z, double heading, bool holdShift, double durationSecs) : base(NavNodeType.Jump, x, y, z)
        {
            Heading = heading;
            HoldShift = holdShift;
            DurationSecs = durationSecs;
        }

        protected override void ReadNodeData(MetaFile file)
        {
            Heading = file.ReadNextLineAsDouble();
            HoldShift = file.ReadNextLineAsBoolean();
            DurationSecs = file.ReadNextLineAsDouble();
        }

        protected override void WriteNodeData(MetaFileBuilder writer)
        {
            writer.WriteLine(Heading.ToString());
            writer.WriteLine(HoldShift ? "True" : "False");
            writer.WriteLine(DurationSecs.ToString());
        }
    }

    public static class NavRoutes
    {
        public static readonly string NavRouteHeader = "uTank2 NAV 1.2";

        public static VTNavRoute LoadNavRoute(MetaFile file)
        {
            string header = file.ReadNextLineAsString();
            if (header != NavRouteHeader)
                throw new ArgumentException($"Unexpected header on first line of nav route (\"{header}\") expected: {NavRouteHeader}");
            int routeTypeId = file.ReadNextLineAsInt();
            NavRouteType routeType = (NavRouteType)routeTypeId;
            VTNavRoute navRoute = routeType.NewRoute();
            navRoute.ReadFrom(file);
            return navRoute;
        }

        public static VTNavRoute ParseEmbeddedNavRoute(string content)
        {
            List<string> lines = content.Split('\n').ToList();
            if (lines.Count == 0)
                throw new ArgumentException("Unable to parse empty content as a nav route!");
            string name = lines[0];
            MetaFile file = new MetaFile(MetaFileType.NavRoute, name, lines);
            // eat the name we already handled
            file.ReadNextLineAsString();

            // capture node count to validate after reading all of them
            int nodeCount = file.ReadNextLineAsInt();

            VTNavRoute navRoute = LoadNavRoute(file);
            if (navRoute is NavListRoute)
            {
                NavListRoute navList = navRoute as NavListRoute;
                if (navList.NodeCount != nodeCount)
                    throw new ArgumentException($"Expected to read {nodeCount} nodes for Nav[{name}] but only read {navList.NodeCount}");
            }

            // all done
            return navRoute;
        }
    }

    public static class VTNavNodeExtensions
    {
        public static VTNavRoute NewRoute(this NavRouteType routeType)
        {
            switch (routeType)
            {
                case NavRouteType.Follow: return new NavFollowRoute();
                case NavRouteType.Linear: return new NavListRoute(NavRouteType.Linear);
                case NavRouteType.Circular: return new NavListRoute(NavRouteType.Circular);
                case NavRouteType.Once: return new NavListRoute(NavRouteType.Once);
                default:
                    throw new ArgumentException($"Unknown nav route type {routeType} ({(int)routeType})");
            }
        }

        public static VTNavNode NewNode(this NavNodeType nodeType)
        {
            switch (nodeType)
            {
                case NavNodeType.Point: return new NavNodePoint();
                case NavNodeType.Recall: return new NavNodeRecall();
                case NavNodeType.Pause: return new NavNodePause();
                case NavNodeType.Chat: return new NavNodeChatCommand();
                case NavNodeType.OpenVendor: return new NavNodeOpenVendor();
                case NavNodeType.UseObject: return new NavNodeUseObject();
                case NavNodeType.TalkToNPC: return new NavNodeTalkToNPC();
                case NavNodeType.Checkpoint: return new NavNodeCheckpoint();
                case NavNodeType.Jump: return new NavNodeJump();

                case NavNodeType.Portal:
                    // unsupported node type!
                    return new NavNodePortal();

                default:
                    throw new ArgumentException($"Unable to find NavNodeType {nodeType} ({(int)nodeType})");
            }
        }
    }
}
