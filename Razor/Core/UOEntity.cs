using System.Collections.Generic;
using System.IO;

namespace Assistant
{
    public class UOEntity
    {
        private ushort m_Hue;
        protected ObjectPropertyList m_ObjPropList;
        private Point3D m_Pos;

        public UOEntity(BinaryReader reader, int version)
        {
            Serial = reader.ReadUInt32();
            m_Pos = new Point3D(reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32());
            m_Hue = reader.ReadUInt16();
            Deleted = false;

            m_ObjPropList = new ObjectPropertyList(this);
        }

        public UOEntity(Serial ser)
        {
            m_ObjPropList = new ObjectPropertyList(this);

            Serial = ser;
            Deleted = false;
        }

        public ObjectPropertyList ObjPropList => m_ObjPropList;

        public Serial Serial { get; }

        public virtual Point3D Position
        {
            get => m_Pos;
            set
            {
                if (value != m_Pos)
                {
                    var oldPos = m_Pos;
                    m_Pos = value;
                    OnPositionChanging(oldPos);
                }
            }
        }

        public bool Deleted { get; private set; }

        public Dictionary<ushort, ushort> ContextMenu { get; } = new Dictionary<ushort, ushort>();

        public virtual ushort Hue
        {
            get => m_Hue;
            set => m_Hue = value;
        }

        public int OPLHash
        {
            get
            {
                if (m_ObjPropList != null)
                    return m_ObjPropList.Hash;

                return 0;
            }
            set
            {
                if (m_ObjPropList != null)
                    m_ObjPropList.Hash = value;
            }
        }

        public bool ModifiedOPL => m_ObjPropList.Customized;

        public virtual void SaveState(BinaryWriter writer)
        {
            writer.Write((uint) Serial);
            writer.Write(m_Pos.X);
            writer.Write(m_Pos.Y);
            writer.Write(m_Pos.Z);
            writer.Write(m_Hue);
        }

        public virtual void AfterLoad()
        {
        }

        public virtual void Remove()
        {
            Deleted = true;
        }

        public virtual void OnPositionChanging(Point3D oldPos)
        {
        }

        public override int GetHashCode()
        {
            return Serial.GetHashCode();
        }

        public void ReadPropertyList(PacketReader p)
        {
            m_ObjPropList.Read(p);
        }

        /*public Packet BuildOPLPacket()
        { 
            return m_ObjPropList.BuildPacket();
        }*/

        public void OPLChanged()
        {
            //ClientCommunication.SendToClient( m_ObjPropList.BuildPacket() );
            ClientCommunication.SendToClient(new OPLInfo(Serial, OPLHash));
        }
    }
}