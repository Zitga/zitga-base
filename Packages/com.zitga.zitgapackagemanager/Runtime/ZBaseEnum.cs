namespace ZitgaPackageManager
{
    public class ZBaseEnum
    {
        public enum Status
        {
            installed = 1,
            none = 2,
            updated = 3
        }

        public enum Source
        {
            registry,
            git,
            embedded
        }
    }
}
