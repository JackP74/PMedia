using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMedia
{
    public class MediaQuery
    {
        public string Name;
        public string Info;
        public string Position;

        public MediaQuery(string Name, string Info, string Position)
        {
            this.Name = Name;
            this.Info = Info;
            this.Position = Position;
        }
    }
}
