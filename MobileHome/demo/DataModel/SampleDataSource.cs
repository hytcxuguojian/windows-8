using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.Data.Json;
using Windows.Storage;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

// 此文件定义的数据模型可充当在添加、移除或修改成员时
// 。  所选属性名称与标准项模板中的数据绑定一致。
//
// 应用程序可以使用此模型作为起始点并以它为基础构建，或完全放弃它并
// 替换为适合其需求的其他内容。如果使用此模式，则可提高应用程序
// 响应速度，途径是首次启动应用程序时启动 App.xaml 隐藏代码中的数据加载任务
//。

namespace demo.Data
{
    /// <summary>
    /// 泛型项数据模型。
    /// </summary>
    public class SampleDataItem
    {
        public SampleDataItem(String uniqueId, String title, String subtitle, String imagePath, String description, String content, double preparationTime, double rating, bool favorite, string tileImagePath, ObservableCollection<string> ingredients, SampleDataGroup group)
        {
            this.UniqueId = uniqueId;
            this.Title = title;
            this.Subtitle = subtitle;
            this.Description = description;
            this.ImagePath = imagePath;
            this.Content = content;
            this.PreparationTime = preparationTime;
            this.Rating = rating;
            this.Favorite = favorite;
            this.TileImagePath = tileImagePath;
            this.Ingredients = ingredients;
            this.Group = group;
        }

        public string UniqueId { get; private set; }
        public string Title { get; private set; }
        public string Subtitle { get; private set; }
        public string Description { get; private set; }
        public string ImagePath { get; private set; }
        public string Content { get; private set; }
        public double PreparationTime { get; private set; }
        public double Rating { get; private set; }
        public bool Favorite { get; private set; }
        public string TileImagePath { get; private set; }
        public ObservableCollection<string> Ingredients { get; private set; }
        public SampleDataGroup Group { get; private set; }

        public override string ToString()
        {
            return this.Title;
        }
    }

    /// <summary>
    /// 泛型组数据模型。
    /// </summary>
    public class SampleDataGroup
    {
        public SampleDataGroup(String uniqueId, String title, String subtitle, String imagePath, String description, string groupImagePath, string groupHeaderImagePath)
        {
            this.UniqueId = uniqueId;
            this.Title = title;
            this.Subtitle = subtitle;
            this.Description = description;
            this.ImagePath = imagePath;
            this.Items = new ObservableCollection<SampleDataItem>();
            this.GroupImagePath = groupImagePath;
            this.GroupHeaderImagePath = groupHeaderImagePath;
        }

        public string UniqueId { get; private set; }
        public string Title { get; private set; }
        public string Subtitle { get; private set; }
        public string Description { get; private set; }
        public string ImagePath { get; private set; }
        public string GroupImagePath { get; private set; }
        public string GroupHeaderImagePath { get; private set; }
        public ObservableCollection<SampleDataItem> Items { get; private set; }

        public override string ToString()
        {
            return this.Title;
        }
    }

    /// <summary>
    /// 创建包含从静态 json 文件读取内容的组和项的集合。
    /// 
    /// SampleDataSource 通过从项目中包括的静态 json 文件读取的数据进行
    /// 初始化。 这样在设计时和运行时均可提供示例数据。
    /// </summary>
    public sealed class SampleDataSource
    {
        private static SampleDataSource _sampleDataSource = new SampleDataSource();

        private ObservableCollection<SampleDataGroup> _groups = new ObservableCollection<SampleDataGroup>();
        public ObservableCollection<SampleDataGroup> Groups
        {
            get { return this._groups; }
        }

        public static async Task<IEnumerable<SampleDataGroup>> GetGroupsAsync()
        {
            await _sampleDataSource.GetSampleDataAsync();

            return _sampleDataSource.Groups;
        }

        public static async Task<SampleDataGroup> GetGroupAsync(string uniqueId)
        {
            await _sampleDataSource.GetSampleDataAsync();
            // 对于小型数据集可接受简单线性搜索
            var matches = _sampleDataSource.Groups.Where((group) => group.UniqueId.Equals(uniqueId));
            if (matches.Count() == 1) return matches.First();
            return null;
        }

        public static async Task<SampleDataItem> GetItemAsync(string uniqueId)
        {
            await _sampleDataSource.GetSampleDataAsync();
            // 对于小型数据集可接受简单线性搜索
            var matches = _sampleDataSource.Groups.SelectMany(group => group.Items).Where((item) => item.UniqueId.Equals(uniqueId));
            if (matches.Count() == 1) return matches.First();
            return null;
        }

        private async Task GetSampleDataAsync()
        {
            if (this._groups.Count != 0)
                return;

            Uri dataUri = new Uri("ms-appx:///DataModel/SampleData.json");

            StorageFile file = await StorageFile.GetFileFromApplicationUriAsync(dataUri);
            string jsonText = await FileIO.ReadTextAsync(file);
            JsonObject jsonObject = JsonObject.Parse(jsonText);
            JsonArray jsonArray = jsonObject["Groups"].GetArray();

            foreach (JsonValue groupValue in jsonArray)
            {
                JsonObject groupObject = groupValue.GetObject();
                SampleDataGroup group = new SampleDataGroup(groupObject["UniqueId"].GetString(),
                                                            groupObject["Title"].GetString(),
                                                            groupObject["Subtitle"].GetString(),
                                                            groupObject["ImagePath"].GetString(),
                                                            groupObject["Description"].GetString(),
                                                            groupObject[ "GroupImagePath" ].GetString(), 
                                                            groupObject[ "GroupHeaderImagePath" ].GetString());

                foreach (JsonValue itemValue in groupObject["Items"].GetArray())
                {
                    JsonObject itemObject = itemValue.GetObject();
                    group.Items.Add(new SampleDataItem(itemObject["UniqueId"].GetString(),
                                                       itemObject["Title"].GetString(),
                                                       itemObject["Subtitle"].GetString(),
                                                       itemObject["ImagePath"].GetString(),
                                                       itemObject["Description"].GetString(),
                                                       itemObject["Content"].GetString(),
                                                       itemObject["PreparationTime"].GetNumber(),
                                                        itemObject["Rating"].GetNumber(),
                                                        itemObject["Favorite"].GetBoolean(),
                                                        itemObject["TileImagePath"].GetString(),
                                                        new ObservableCollection<string>(itemObject["Ingredients"].GetArray().Select(p => p.GetString())),group));
                }
                this.Groups.Add(group);
            }
        }

        public static async Task<SampleDataGroup> GetTopRatedRecipesAsync(int count)
        {
            await _sampleDataSource.GetSampleDataAsync();

            var favorites = new SampleDataGroup("TopRated", "Top Rated", "Top Rated Recipes", "", "Favorite recipes rated by our users.", "", "");
            var topRatedRecipes = _sampleDataSource.Groups.SelectMany(group => group.Items).OrderByDescending(recipe => recipe.Rating).Take(count);
            foreach (var recipe in topRatedRecipes)
            {
                favorites.Items.Add(recipe);
            }

            return favorites;
        }

        public static async Task<IEnumerable<SampleDataItem>> GetFavoriteRecipesAsync(int count)
        {
            await _sampleDataSource.GetSampleDataAsync();
            return _sampleDataSource.Groups.SelectMany(group => group.Items).Where(recipe => recipe.Favorite).Take(count);
        }

        public static IEnumerable<SampleDataGroup> Search(string searchText, bool titleOnly = false)
        {
            var query = searchText.ToUpperInvariant();
            _sampleDataSource.GetSampleDataAsync().Wait();
            return _sampleDataSource.Groups
                    .Select(group =>
                    {
                        var filteredGroup = new SampleDataGroup(group.UniqueId, group.Title, group.Subtitle, group.ImagePath, group.Description, group.GroupImagePath, group.GroupHeaderImagePath);

                        // add recipes that contain search text in title or content
                        foreach (var item in group.Items
                                    .Where(item => item.Title.ToUpperInvariant().Contains(query) || (!titleOnly && item.Content.ToUpperInvariant().Contains(query))))
                        {
                            filteredGroup.Items.Add(item);
                        }

                        return filteredGroup;
                    });
        }
    }
}