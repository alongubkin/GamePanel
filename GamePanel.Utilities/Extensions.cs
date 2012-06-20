using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;

namespace GamePanel.Utilities
{
    public static class SlotUtils
    {
        public static IEnumerable<SelectListItem> GenerateDropDownList(int steps, int priceDelta)
        {
            for (int x = 10; x <= 64; x += steps)
            {
                yield return new SelectListItem()
                {
                    Value = x.ToString(),
                    Text = string.Concat(x,  x - 10 == 0 ? string.Empty : " (add " + ((x - 11) * priceDelta).ToString() + " credits)")
                };
            }
        }
    }
}
