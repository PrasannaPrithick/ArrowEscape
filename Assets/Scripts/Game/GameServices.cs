using System;

namespace HueGates.Game
{
    public interface IAds
    {
        void ShowInterstitial(Action closed);
        void ShowRewarded(string placement, Action<bool> completed);
    }

    public interface IStore
    {
        string Price(string productId);
        void Purchase(string productId, Action<bool> completed);
    }

    public sealed class ProductInfo
    {
        public string id;
        public string title;
        public string detail;
        public string price;
        public bool consumable;
    }

    public static class ShopCatalog
    {
        public static readonly ProductInfo[] Products =
        {
            new ProductInfo { id = "remove_ads", title = "Remove ads", detail = "Turns off interstitials. Rewarded ads stay optional.", price = "$2.99", consumable = false },
            new ProductInfo { id = "hint_pack_10", title = "Hint pack", detail = "+10 hints", price = "$0.99", consumable = true },
            new ProductInfo { id = "undo_pack_10", title = "Undo pack", detail = "+10 undos", price = "$0.99", consumable = true },
            new ProductInfo { id = "picture_pack_1", title = "Picture pack", detail = "+20 extra gallery pictures", price = "$1.99", consumable = false },
            new ProductInfo { id = "starter_bundle", title = "Starter bundle", detail = "Remove ads, 20 hints, 20 undos, and the picture pack.", price = "$4.99", consumable = false }
        };

        public static ProductInfo Find(string id)
        {
            for (int i = 0; i < Products.Length; i++)
            {
                if (Products[i].id == id) return Products[i];
            }
            return null;
        }
    }

    /// <summary>
    /// Editor and offline stand-in. A store build can replace this with Play Billing.
    /// </summary>
    public sealed class FakeStore : IStore
    {
        public string Price(string productId)
        {
            ProductInfo info = ShopCatalog.Find(productId);
            return info == null ? "" : info.price;
        }

        public void Purchase(string productId, Action<bool> completed)
        {
            if (completed != null) completed(true);
        }
    }

    /// <summary>
    /// Editor and offline stand-in. A store build can replace this with AdMob.
    /// The presenter shows the placeholder ad and reports whether the reward was collected.
    /// </summary>
    public sealed class FakeAds : IAds
    {
        public bool Removed;
        readonly Action<bool, string, Action<bool>> present;

        public FakeAds(Action<bool, string, Action<bool>> present)
        {
            this.present = present;
        }

        public void ShowInterstitial(Action closed)
        {
            if (Removed)
            {
                if (closed != null) closed();
                return;
            }
            present(false, "interstitial", _ => { if (closed != null) closed(); });
        }

        public void ShowRewarded(string placement, Action<bool> completed)
        {
            present(true, placement, completed);
        }
    }
}
