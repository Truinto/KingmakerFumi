using Kingmaker.UI._ConsoleUI.ActionBar;
using Kingmaker.UI.UnitSettings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpellPouchKing
{
    /// <summary>
    /// overwrites logic to use any MechanicActionBarSlot
    /// </summary>
    public class ActionBarConvertedVMAny : ActionBarConvertedVm
    {
        public ActionBarConvertedVMAny(ActionBarSlotVM parent, List<MechanicActionBarSlot> list, Action onClose) : base(new(), onClose)
        {
            foreach (var item in list)
                this.Slots.Add(new ActionBarSlotVMChild(parent, item));
        }
    }
}
