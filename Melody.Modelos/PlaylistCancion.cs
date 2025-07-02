using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Melody.Modelos
{
    public class PlaylistCancion
    {
        public int Id { get; set; }
        public int PlaylistId { get; set; }
        public int CancionId { get; set; }
        public Playlist? Playlist { get; set; }
        public Cancion? Cancion { get; set; }
    }
}
