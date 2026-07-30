using QFramework;

namespace CardGame
{
    public class DrawCardCommand : AbstractCommand
    {
        private readonly int _count;

        public DrawCardCommand(int count)
        {
            _count = count;
        }

        protected override void OnExecute()
        {
            this.GetSystem<ICardSystem>().DrawCards(_count);
        }
    }
}
