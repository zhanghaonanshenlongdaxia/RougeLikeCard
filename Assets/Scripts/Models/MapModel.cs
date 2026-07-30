using QFramework;

namespace CardGame
{
    public class MapModel : AbstractModel, IMapModel
    {
        public GameMap CurrentMap { get; set; }

        protected override void OnInit()
        {
        }
    }
}
