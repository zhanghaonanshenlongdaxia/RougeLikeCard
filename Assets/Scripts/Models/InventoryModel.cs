using System.Collections.Generic;
using System.Linq;
using QFramework;

namespace CardGame
{
    public class InventoryModel : AbstractModel, IInventoryModel
    {
        public List<InventorySlot> Slots { get; } = new List<InventorySlot>();
        public List<InventorySlot> SafeBoxSlots { get; } = new List<InventorySlot>();
        public BindableProperty<int> CurrentWeight { get; } = new BindableProperty<int>(0);
        public BindableProperty<int> MaxWeight { get; } = new BindableProperty<int>(15);
        public int SafeBoxCapacity { get; set; } = 10;

        protected override void OnInit()
        {
        }

        public void UpdateWeight()
        {
            int w = 0;
            foreach (var slot in Slots)
                if (slot != null && !slot.IsEmpty)
                    w += slot.Weight;
            CurrentWeight.Value = w;
        }
    }
}
