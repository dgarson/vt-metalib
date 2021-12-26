using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using VTMetaLib.afy;
using VTMetaLib.VTank;
using VTMetaLib.Data;

namespace VTMetaLib.afy.yaml
{
    

    public class AfyNavRouteDefinition

    {
        /// <summary>
        /// Required name for this nav route, which is referenced by any `EmbedNav` actions throughout the meta.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The type of nav route, which determines which other properties of this object are expected/required to be populated.
        /// 
        /// NOTE: This is a required field only for the inlined nav route defining points inside an `afy` file. If this is not provided, it is inferred
        /// when `Path` or `Data`, or `Follow` fields being provided.
        /// </summary>
        public AfyNavType Type { get; set; }

        /// <summary>
        /// Option to reverse the nav route point sequence
        /// </summary>
        public bool ReversePoints { get; set; } = false;

        /// <summary>
        /// Optional transformation that will be applied to the nav route defined in this node
        /// </summary>
        public RouteTransform Transform { get; set; }

        /// <summary>
        /// The path to an external `nav` file. If this field is provided, then all nav data comes from the loaded file, including the `Type` property.
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// Inlined data block (yaml string block literal) that contains the `nav` file content.
        /// </summary>
        public string Data { get; set; }

        /// <summary>
        /// Used exclusively for a Follow nav. Providing this property will imply the use of `Follow` value for `Type` property.
        /// </summary>
        public string Follow { get; set; }

        /// <summary>
        /// Ordered sequence of nav route properties (in the form of dictionaries) where each one will have a single key/value pair
        /// </summary>
        public List<Dictionary<string, string>> Points { get; set; }

        // TODO ways to convert this to the actual Data for a Nav Route!!! (bytes)
    }

    public enum AfNavPointType
    {

    }

    public interface AfNavPointDefinition
    { 

        public Dictionary<string, string> ToProperties();

        public void SetFromProperties(Dictionary<string, string> props);
    }

    public abstract class CoordinateNavPointDef : AfNavPointDefinition
    {
        public double X { get; set; }

        public double Y { get; set; }

        public double Z { get; set; }

        protected CoordinateNavPointDef() { }

        protected CoordinateNavPointDef(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public string Point
        {
            get
            {
                // TODO: number custom formatting!!
                return $"{Formatting.FormatCoordinate(X)} {Formatting.FormatCoordinate(Y)} {Formatting.FormatCoordinate(Z)}";
            }
            set
            {
                string[] tokens = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                SetPointFromTokens(tokens);
            }
        }

        internal void SetPointFromTokens(string[] tokens)
        {
            if (tokens.Length < 3)
                throw new ArgumentException($"3 or more arguments required for {GetType().Name} but only got {tokens.Length}");
            try
            {
                X = double.Parse(tokens[0]);
                Y = double.Parse(tokens[1]);
                Z = double.Parse(tokens[2]);
            }
            catch (FormatException e)
            {
                throw new ArgumentException($"Unable to parse one or more coordinate tokens as a numerical value: {string.Join(' ', tokens)}", e);
            }
        }

        public abstract Dictionary<string, string> ToProperties();
        public abstract void SetFromProperties(Dictionary<string, string> props);

        protected List<string> NewTokenList()
        {
            List<string> tokens = new List<string>();
            Formatting.AppendCoords(tokens, X, Y, Z);
            return tokens;
        }

        public void SetCoordinates(double x, double y, double z)
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
        }
    }

    public abstract class SinglePropNavPointDef : CoordinateNavPointDef
    {

        internal string UniquePropertyName { get; set; }

        internal string DisplayName { get; set; }

        protected SinglePropNavPointDef(string displayName, string uniquePropName, double x, double y, double z) : base(x, y, z)
        {
            DisplayName = displayName;
            UniquePropertyName = uniquePropName;
        }

        protected SinglePropNavPointDef(string displayName, string uniquePropName)
        {
            DisplayName = displayName;
            UniquePropertyName = uniquePropName;
        }


        public override Dictionary<string, string> ToProperties()
        {
            Dictionary<string, string> props = new Dictionary<string, string>();

            List<string> tokens = NewTokenList();
            AddDataToTokens(tokens);

            // join tokens as one string and add under unique prop
            string strVal = string.Join(' ', tokens);
            props.Add(UniquePropertyName, strVal);

            return props;
        }

        public override void SetFromProperties(Dictionary<string, string> props)
        {
            if (!props.ContainsKey(UniquePropertyName))
                throw new ArgumentException($"Missing required key {UniquePropertyName} for {GetType().Name}");
            string strVal = props[UniquePropertyName];
            if (string.IsNullOrWhiteSpace(strVal))
                throw new ArgumentException($"Key {UniquePropertyName} was provided but had no value for {GetType().Name}");
            string[] tokens = strVal.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            SetPointFromTokens(tokens);

            // child implementation class parsing additional beyond coordinate tuple
            SetDataFromTokens(tokens[3..]);
        }

        internal virtual void AddDataToTokens(List<string> tokens) { }

        internal virtual void SetDataFromTokens(string[] dataTokens) { }
    }

    public abstract class SingleScalarPropNavPointDef : SinglePropNavPointDef
    {
        internal string Value { get; set; }

        protected SingleScalarPropNavPointDef(string displayName, string uniquePropName, double x, double y, double z) : base(displayName, uniquePropName, x, y, z) { }

        protected SingleScalarPropNavPointDef(string displayName, string uniquePropName) : base(displayName, uniquePropName) { }

        internal override void AddDataToTokens(List<string> tokens)
        {
            tokens.Add(Value);
        }

        internal override void SetDataFromTokens(string[] dataTokens)
        {
            if (dataTokens.Length == 0)
                throw new ArgumentException($"Expected exactly one data token for {GetType().Name} value but none were found!");
            Value = dataTokens[0];
        }
    }

    public class ChatNodeDefinition : SingleScalarPropNavPointDef
    {
        public string Message
        {
            get => Value;
            set => Value = value;
        }

        public ChatNodeDefinition() : base("Chat", "Message") { }
        public ChatNodeDefinition(string message) : this()
        {
            Value = message;
        }
    }

    public class VendorNodeDefinition : SinglePropNavPointDef
    {
        private string vendorGuid;

        public string VendorGUID
        {
            get => vendorGuid;
            set => vendorGuid = Formatting.ValidateGUID(value);
        }

        public string VendorName { get; set; }

        public VendorNodeDefinition() : base("Open Vendor", "Vendor") { }

        public VendorNodeDefinition(double x, double y, double z, string vendorGuid, string vendorName) : base("Open Vendor", "Vendor", x, y, z)
        {
            VendorGUID = vendorGuid;
            VendorName = vendorName;
        }

        internal override void AddDataToTokens(List<string> tokens)
        {
            tokens.Add(VendorGUID);
            tokens.Add(VendorName);
        }

        internal override void SetDataFromTokens(string[] tokens)
        {
            VendorGUID = tokens[0];
            VendorName = tokens[1];
        }
    }

    public class PortalNodeDefinition : SingleScalarPropNavPointDef
    {
        public string GUID
        {
            get => Value;
            set => Value = value;
        }

        public PortalNodeDefinition() : base("Portal", "Portal") { }

        public PortalNodeDefinition(double x, double y, double z, string portalGuid) : base("Portal", "Portal", x, y, z)
        {
            GUID = portalGuid;
        }
    }

    public class PointNodeDefinition : SinglePropNavPointDef
    {
        public PointNodeDefinition() : base("Point", "Point") { }

        public PointNodeDefinition(double x, double y, double z) : this()
        {
            SetCoordinates(x, y, z);
        }

        public PointNodeDefinition(string strCoords) : this()
        {
            Point = strCoords;
        }
    }

    public class PauseNodeDefinition : SingleScalarPropNavPointDef
    {
        public double Seconds
        {
            get => double.Parse(Value);
            set => Value = value.ToString();
        }

        public PauseNodeDefinition() : base("Pause", "Seconds") { }

        public PauseNodeDefinition(double x, double y, double z, double seconds) : base("Pause", "Seconds", x, y, z)
        {
            Seconds = seconds;
        }
    }

    public abstract class UseTargetObjectNavPointDef : SinglePropNavPointDef
    {
        public double TargetX { get; set; }

        public double TargetY { get; set; }

        public double TargetZ { get; set; }

        public ObjectClass ObjectClass { get; set; }

        public string TargetName { get; set; }

        public UseTargetObjectNavPointDef(string displayName, string uniquePropName) : base(displayName, uniquePropName) { }

        public UseTargetObjectNavPointDef(string displayName, string uniquePropName, double x, double y, double z, double tx, double ty, double tz, ObjectClass objClass, string targetName) : base(displayName, uniquePropName, x, y, z)
        {
            SetTarget(tx, ty, tz, objClass, targetName);
        }

        public void SetTarget(double tx, double ty, double tz, ObjectClass objClass, string targetName)
        {
            ValidateTarget(objClass, targetName);
            TargetX = tx;
            TargetY = ty;
            TargetZ = tz;
            ObjectClass = objClass;
            TargetName = targetName;
        }

        internal abstract void ValidateTarget(ObjectClass objClass, string targetName);

        internal override void AddDataToTokens(List<string> tokens)
        {
            Formatting.AppendCoords(tokens, TargetX, TargetY, TargetZ);
            tokens.Append(ObjectClass.ToString());
            tokens.Append(TargetName);
        }

        internal override void SetDataFromTokens(string[] dataTokens)
        {
            if (dataTokens.Length < 5)
                throw new ArgumentException($"At least 5 data tokens required for TargetX/TargetY/TargetZ and ObjectClass/TargetName " +
                    $"for UsePortalNpc node, only got {dataTokens.Length}");

            try
            {
                TargetX = double.Parse(dataTokens[0]);
                TargetY = double.Parse(dataTokens[1]);
                TargetZ = double.Parse(dataTokens[2]);
            }
            catch (FormatException e)
            {
                throw new ArgumentException($"Failed to parse one or more tokens in coordinate tuple as numerical value: {string.Join(", ", dataTokens)}", e);
            }

            ObjectClass = ObjectClasses.Parse(dataTokens[3]);
            TargetName = dataTokens[4];
        }

    }


    /// <summary>
    /// TODO: consider having 2 distinct properties, one for a Portal and one for NPC, both aliased/mapped to what is now 'TargetName' except with auto-determined
    /// objectClass ??
    /// </summary>
    public class UsePortalOrNpc : UseTargetObjectNavPointDef
    {
        public UsePortalOrNpc() : base("UsePortalOrNpc", "UsePortalNpc") { }

        public UsePortalOrNpc(string displayName, string uniquePropName, double x, double y, double z, double tx, double ty, double tz, ObjectClass objClass, string targetName)
            : base(displayName, uniquePropName, x, y, z, tx, ty, tz, objClass, targetName) { }


        internal override void ValidateTarget(ObjectClass objClass, string targetName)
        {
            if (objClass != ObjectClass.NPC && objClass != ObjectClass.Container && objClass != ObjectClass.Portal)
                throw new ArgumentException($"UsePortalOrNpc point only valid for NPC, Portal and Container ObjectClasses, " +
                    $"but got {objClass} for target: {targetName}");
        }

        public string Portal
        {
            get => TargetName;
            set
            {
                ObjectClass = ObjectClass.Portal;
                TargetName = value;
            }
        }

        public string Container
        {
            get => TargetName;
            set
            {
                ObjectClass = ObjectClass.Container;
                TargetName = value;
            }
        }

        public string NPC
        {
            get => TargetName;
            set
            {
                ObjectClass = ObjectClass.NPC;
                TargetName = value;
            }
        }
    }

    public class TalkToNPCNodeDefinition : UseTargetObjectNavPointDef
    {
        public TalkToNPCNodeDefinition() : base("TalkToNPC", "NpcTalk") { }

        public TalkToNPCNodeDefinition(string displayName, string uniquePropName, double x, double y, double z, double tx, double ty, double tz, string targetName)
            : base(displayName, uniquePropName, x, y, z, tx, ty, tz, ObjectClass.NPC, targetName) { }

        internal override void ValidateTarget(ObjectClass objClass, string targetName)
        {
            if (objClass != ObjectClass.NPC)
                throw new ArgumentException($"Non-NPC object class given for TalkToNPC nav point: {objClass} with target: {targetName}");
        }
    }

    public class CheckpointNodeDefinition : SinglePropNavPointDef
    {
        public CheckpointNodeDefinition() : base("Nav Checkpoint", "Checkpoint") { }

        public CheckpointNodeDefinition(double x, double y, double z) : this()
        {
            SetCoordinates(x, y, z);
        }

        public CheckpointNodeDefinition(string strCoords) : this()
        {
            Point = strCoords;
        }
    }

    public class JumpNodeDefinition : SinglePropNavPointDef
    {
        public double Heading { get; set; }

        public bool HoldShift { get; set; }

        public double Seconds { get; set; }

        public JumpNodeDefinition() : base("Jump", "Jump") { }

        public JumpNodeDefinition(double x, double y, double z, double heading, bool holdShift, double seconds) : base("Jump", "Jump", x, y, z)
        {
            Heading = heading;
            HoldShift = holdShift;
            Seconds = seconds;
        }

        internal override void AddDataToTokens(List<string> tokens)
        {
            tokens.Add(Formatting.FormatHeading(Heading));
            tokens.Add(HoldShift ? "True" : "False");
            tokens.Add(Formatting.FormatDuration(Seconds));
        }

        internal override void SetDataFromTokens(string[] dataTokens)
        {
            if (dataTokens.Length < 3)
                throw new ArgumentException($"3 arguments required for Jump nav point, but only got {dataTokens.Length}");
            try
            {
                Heading = double.Parse(dataTokens[0]);
                Seconds = double.Parse(dataTokens[2]);
            }
            catch (FormatException e)
            {
                throw new ArgumentException($"Unable to parse either heading or duration in " +
                    $"seconds (delay) to a number value: {string.Join(", ", dataTokens)}", e);
            }
            if (bool.TryParse(dataTokens[1].ToLower(), out bool holdShift))
                HoldShift = holdShift;
            else
                throw new ArgumentException($"non-boolean value for HoldShift in Jump nav " +
                    $"point: {dataTokens[1]} (parsed as '{dataTokens[1].ToLower()}')");
        }
    }

    public class RouteTransform
    {
        public Double dx;
        public Double dy;
        // optional, will never fail null-check
        public Double dz = 0d;
    }
}
