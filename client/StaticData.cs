using System.Text.Encodings.Web;
using System.Text.Json;
using System.Xml.Linq;
using Lumina;
using Lumina.Data;
using Lumina.Data.Files;
using Lumina.Excel;
using Lumina.Excel.Sheets;

namespace ffxiv_dresser_analyze_client
{
    internal class StaticData
    {
        public static readonly JsonSerializerOptions JsonOptions = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
        public static readonly byte[] BmpHeader = [
            0x42, 0x4D, 0x7A, 0x19, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x7A, 0x00, 0x00, 0x00, 0x6C, 0x00,
            0x00, 0x00, 0x28, 0x00, 0x00, 0x00, 0xD8, 0xFF, 0xFF, 0xFF, 0x01, 0x00, 0x20, 0x00, 0x03, 0x00,
            0x00, 0x00, 0x00, 0x19, 0x00, 0x00, 0x60, 0x09, 0x00, 0x00, 0x60, 0x09, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFF, 0x00, 0x00, 0xFF, 0x00, 0x00, 0xFF, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0xFF,
            // Firefox 不支持只带 alpha mask 不带后续 color space 内容的 DIB header，加个无效的 bV4CSType=-1 哄哄它
            // https://hg.mozilla.org/mozilla-central/file/tip/image/decoders/nsBMPDecoder.cpp
            0xFF, 0xFF, 0xFF, 0xFF, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 
        ];

        public readonly Dictionary<string, byte[]> Jsons = [];

        private readonly GameData lumina;
        private readonly Category uiPack;

        private readonly ushort[] itemIconIds;
        private readonly HashSet<uint> cabinetItemIdSet;
        private int[]? itemCategories;

        public StaticData(string sqpackPath)
        {
            lumina = new GameData(sqpackPath, new() { DefaultExcelLanguage = Lumina.Data.Language.ChineseSimplified });
            uiPack = lumina.Repositories["ffxiv"].Categories[6][0];

            var sItem = lumina.GetExcelSheet<Item>()!;
            itemIconIds = sItem.Select(eItem => eItem.Icon).ToArray();
            cabinetItemIdSet = lumina.GetExcelSheet<Cabinet>()!.Select(e => e.Item.RowId).ToHashSet();

            AddJson("/data/outfits", GenerateOutfits());
            AddJson("/data/cabinets", GenerateCabinets());
            AddJson("/data/reclaims", GenerateReclaims());
            AddJson("/data/dyes", GenerateDyes());
        }

        public byte[]? GetIcon(uint itemId, bool hq)
        {
            if (itemId >= itemIconIds.Length) return null;
            var iconId = itemIconIds[itemId].ToString().PadLeft(6, '0');
            var iconPath = $"ui/icon/{iconId[..3]}000/{(hq ? "hq/" : "")}{iconId}.tex";
            var hash = GameData.GetFileHash(iconPath);
            var file = uiPack.GetFile<TexFile>(hash);
            return file?.ImageData;
        }

        private void AddJson(string path, dynamic content)
        {
            Jsons.Add(path, JsonSerializer.SerializeToUtf8Bytes(content, JsonOptions));
        }

        private static ulong GetSortKey(Item eItem)
        {
            return (((10000 - eItem.LevelItem.RowId) * 10000u + eItem.Unknown4) * 100000u + eItem.RowId);
        }

        private dynamic GenerateOutfits()
        {
            var sItem = lumina.GetExcelSheet<Item>()!;
            var outfits = new List<(ulong SortKey, object Value)>();
            foreach (var eMirageStoreSetItem in lumina.GetExcelSheet<RawRow>(name: "MirageStoreSetItem")!)
            {
                var id = eMirageStoreSetItem.RowId;
                if (id == 0) continue;
                var name = sItem[id].Name.ToString();

                var items = new List<object>();
                var cabinet = true;
                ulong? sortKey = null;
                for (var i = 0; i < 9; i++)
                {
                    var itemId = (uint)eMirageStoreSetItem.ReadColumn(2 + i);
                    if (itemId == 0) continue;
                    var eItem = sItem[itemId];
                    var itemName = eItem.Name.ToString();
                    var dyeCount = eItem.DyeCount;
                    items.Add(new { id = itemId, name = itemName, dyeCount });
                    cabinet = cabinet && cabinetItemIdSet.Contains(itemId);
                    sortKey ??= GetSortKey(eItem);
                }

                if (items.Count == 0) continue;
                outfits.Add((sortKey ?? 0, new { id, name, items, cabinet }));
            }
            outfits.Sort((a, b) => a.SortKey.CompareTo(b.SortKey));
            return outfits.Select(x => x.Value).ToArray();
        }

        private dynamic GenerateCabinets()
        {
            return GenerateCategorized(lumina.GetExcelSheet<Cabinet>()!.Select(e => e.Item.Value));
        }
        private dynamic GenerateReclaims()
        {
            var sGilShopInfo = lumina.GetExcelSheet<GilShopInfo>()!;
            return GenerateCategorized(lumina.GetExcelSheet<Item>()!.Where(eItem =>
            {
                if (!eItem.IsGlamorous) return false;
                if (!eItem.IsUntradable) return false;
                if (eItem.PriceMid > 1000) return false;
                if (cabinetItemIdSet.Contains(eItem.RowId)) return false;
                if (sGilShopInfo[eItem.RowId].Unknown0 != 1) return false;
                return true;
            }));

        }
        private dynamic GenerateCategorized(IEnumerable<Item> items)
        {
            itemCategories ??= lumina.GetExcelSheet<RawRow>(name: "EquipSlotCategory")!.Select(e =>
            {
                for (var i = 0; i < e.Columns.Count; i++)
                {
                    if ((sbyte)e.ReadColumn(i) == 1) return i + 1;
                }
                return 0;
            }).ToArray();

            var ret = items
                .Where(eItem => eItem.Name.ByteLength > 0)
                .Select(eItem =>
                {
                    var id = eItem.RowId;
                    var name = eItem.Name.ToString();
                    var dyeCount = eItem.DyeCount;
                    var category = itemCategories[eItem.EquipSlotCategory.RowId];
                    var sortKey = GetSortKey(eItem);
                    return new { id, name, dyeCount, category, sortKey };
                })
                .GroupBy(x => x.category)
                .Select(group =>
                {
                    var category = group.Key;
                    var entries = group.ToList();
                    entries.Sort((a, b) => a.sortKey.CompareTo(b.sortKey));
                    var items = entries.Select(x => new { x.id, x.name, x.dyeCount }).ToArray();
                    return new { category, items };
                }).ToList();
            ret.Sort((a, b) => a.category - b.category);
            return ret;
        }

        private dynamic GenerateDyes()
        {
            return lumina.GetExcelSheet<Stain>()!.Select(eStain =>
            {
                var id = eStain.RowId;
                var name = eStain.Name.ToString();
                var color = eStain.Color.ToString("X6");
                return new { id, name, color };
            });
        }
    }
}
