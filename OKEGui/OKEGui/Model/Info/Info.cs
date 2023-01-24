using OKEGui.Utils;
using System;

namespace OKEGui.Model
{
    public enum MuxOption
    {
        Default,
        Mka,
        External,
        ExtractOnly,
        Skip
    }

    public enum InfoType
    {
        Default,
        Video,
        Audio
    }

    public class Info : ICloneable
    {
        // base class of all info object
        public InfoType InfoType { get; protected set; } = InfoType.Default; 
        public MuxOption MuxOption = MuxOption.Default;
        public string Language = Constants.language;
        public string Name = "";
        public bool Optional = false;
        public int Order = Int32.MaxValue;
        private bool _dupOrEmpty;
        public bool DupOrEmpty
        {
            get { return _dupOrEmpty; }
            set
            {
                _dupOrEmpty = value;
                if (value)
                {
                    switch (MuxOption)
                    {
                        case MuxOption.Default:
                        case MuxOption.Mka:
                        case MuxOption.External:
                            MuxOption = MuxOption.ExtractOnly;
                            break;
                        default:
                            break;
                    }
                }
            }
        }

        public virtual Object Clone()
        {
            Info clone = this.MemberwiseClone() as Info;
            HandleCloned(clone);
            return clone;
        }


        protected virtual void HandleCloned(Info clone)
        {
        }
    }
}
