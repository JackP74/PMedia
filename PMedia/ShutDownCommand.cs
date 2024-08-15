namespace PMedia
{
    public class ShutDownCommand
    {
        public ShutDownType shutDownType;
        public int Arg;

        public enum ShutDownType
        {
            Cancel = 0,
            After = 1,
            AfterN = 2,
            In = 3,
            End = 4,
            None = 5
        }

        public ShutDownCommand(ShutDownType shutDownType, int Arg)
        {
            this.shutDownType = shutDownType;
            this.Arg = Arg;
        }
    }
}
