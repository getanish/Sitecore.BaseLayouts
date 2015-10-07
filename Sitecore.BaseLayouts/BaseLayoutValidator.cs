using System;
using Sitecore.Data.Managers;

namespace Sitecore.BaseLayouts
{
    using System.Collections.Generic;

    using Sitecore.Data;
    using Sitecore.Data.Fields;
    using Sitecore.Data.Items;
    using Sitecore.Diagnostics;

    public class BaseLayoutValidator : IBaseLayoutValidator
    {
        /// <summary>
        /// Determines if there is a circular reference in the base layout chain.
        /// </summary>
        /// <param name="item">the item</param>
        /// <returns>True if the item's base layout chain contains a circular reference.  Otherwise, false.</returns>
        public virtual bool HasCircularBaseLayoutReference(BaseLayoutItem item)
        {
            Assert.ArgumentNotNull(item, "item");
            return HasDuplicateBaseLayout(item, new HashSet<ID>());
        }

        /// <summary>
        /// Determines if a circular referece would be created if baseLayoutItem were set
        /// as the base layout for item
        /// </summary>
        /// <param name="item">the item </param>
        /// <param name="baseLayoutItem">the candidate base layout item</param>
        /// <returns>True if a circular reference would be created. Otherwise, false.</returns>
        public virtual bool CreatesCircularBaseLayoutReference(BaseLayoutItem item, Item baseLayoutItem)
        {
            Assert.ArgumentNotNull(item, "item");
            Assert.ArgumentNotNull(baseLayoutItem, "baseLayoutItem");
            Assert.ArgumentCondition(TemplateManager.IsFieldPartOfTemplate(BaseLayoutSettings.FieldId, item), "item",
                "item does not have a Base Layout field");

            return HasDuplicateBaseLayout(baseLayoutItem, new HashSet<ID> {item.ID});
        }

        /// <summary>
        /// Determines if there are duplicat IDs in the baseLayouts ID set and the chain of base layout IDs originating at item.
        /// </summary>
        /// <param name="item">the item</param>
        /// <param name="baseLayouts">the starting set of IDs</param>
        /// <returns>True if there are duplicate IDs.  False if not.</returns>
        protected virtual bool HasDuplicateBaseLayout(BaseLayoutItem item, HashSet<ID> baseLayouts)
        {
            var item2 = item;
            do
            {
                if (!baseLayouts.Add(item2.ID))
                {
                    return true;
                }
                
                item2 = item2.BaseLayout;
            }
            while (item2 != null);

            return false;
        }
    }
}