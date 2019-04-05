using System;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CashPictures
{
	public class CacheManager : ICacheManager
	{
		#region Private fields

		private IRandomAwaiter _randomAwaiter;
		private ICacheKeeper _cacheKeeper;

		#endregion

		#region Properties

		public NewPicture DefaultPicture { get; set; }

		#endregion

		#region Constructor

		public CacheManager(IRandomAwaiter randomAwaiter, ICacheKeeper cacheKeeper)
		{
			DefaultPicture = new NewPicture(ConfigurationManager.AppSettings["DefaultImage"]);

			_randomAwaiter = randomAwaiter;
			_cacheKeeper = cacheKeeper;
		}

		#endregion

		#region Methods

		public Task<NewPicture> GeneratePicture(string uri)
		{
			if (_cacheKeeper.AllCache.Any(x => x.Key == GetFileSize(uri)))
				return ReturnAlreadyGenerated(uri);

			var key = GetFileSize(uri);

			CancellationTokenSource cts = new CancellationTokenSource();

			var task = Task.Factory.StartNew<NewPicture>
			(
				((obj) =>
				{
					_randomAwaiter.ThreadSleep();

					var newPicture = new NewPicture(obj.ToString());

					return newPicture;
				}),
				uri,
				cts.Token
			);

			return Task.Factory.StartNew<NewPicture>(() =>
			{
				if (Task.WaitAll(new[] { task }, TimeSpan.FromSeconds(_cacheKeeper.MaxDownloadingTIme)))
				{
					var resultPicture = task.Result;

					_cacheKeeper.AllCache.Add(new KeyValuePair<long, NewPicture>(key, resultPicture));
				}
				else
				{
					cts.Cancel();

					_cacheKeeper.AllCache.Add(new KeyValuePair<long, NewPicture>(key, DefaultPicture));
				}

				if (_cacheKeeper.AllCache.Count > _cacheKeeper.MaxCapacity)
					_cacheKeeper.AllCache.RemoveAt(0);

				return _cacheKeeper.AllCache.Last().Value;
			});
		}

		public Task<NewPicture> ReturnAlreadyGenerated(string uri)
		{
			return Task.Factory.StartNew<NewPicture>
			(
				obj =>
				{
					var fileSize = GetFileSize(obj.ToString());

					return _cacheKeeper.AllCache.First(x => x.Key == fileSize).Value;
				}, uri
			);
		}

		public long GetFileSize(string uri)
		{
			return new FileInfo(uri).Length;
		}

		public void Save()
		{
			_cacheKeeper.Save();
		}

		#endregion
	}

	public interface ICacheManager
	{
		NewPicture DefaultPicture { get; set; }

		Task<NewPicture> GeneratePicture(string uri);

		Task<NewPicture> ReturnAlreadyGenerated(string uri);

		long GetFileSize(string uri);

		void Save();
	}

	public class RandomAwaiter : IRandomAwaiter
	{
		#region Properties

		public int MinWaitTime { get; set; }
		public int MaxWaitTime { get; set; }
		public Random Random { get; set; }

		#endregion

		#region Methods

		public void ThreadSleep()
		{
			Thread.Sleep(TimeSpan.FromSeconds(Random.Next(MinWaitTime, MaxWaitTime)));
		}

		public RandomAwaiter()
		{
			MinWaitTime = int.Parse(ConfigurationManager.AppSettings["MinWaitTime"]);
			MaxWaitTime = int.Parse(ConfigurationManager.AppSettings["MaxWaitTime"]);
			Random = new Random();
		}

		#endregion
	}

	public interface IRandomAwaiter
	{
		Random Random { get; set; }
		int MinWaitTime { get; set; }
		int MaxWaitTime { get; set; }

		void ThreadSleep();
	}

	public interface ICacheKeeper
	{
		#region Properties

		List<KeyValuePair<long, NewPicture>> AllCache { get; set; }
		int MaxCapacity { get; set; }
		int MaxDownloadingTIme { get; set; }

		#endregion

		#region Methods

		void Save();
		void Load();

		#endregion
	}

	[Serializable()]
	public class CacheKeeper : ICacheKeeper
	{
		#region Properties

		public List<KeyValuePair<long, NewPicture>> AllCache { get; set; }
		public int MaxCapacity { get; set; }
		public int MaxDownloadingTIme { get; set; }

		#endregion

		#region Methods

		public void Save()
		{
			try
			{
				using (var serializationStream = new FileStream("cache_keeper", FileMode.Create, FileAccess.Write))
				{
					BinaryFormatter formatter = new BinaryFormatter();
					formatter.Serialize(serializationStream, this);

					serializationStream.Close();
				}
			}
			catch (Exception e)
			{
			}
		}

		public void Load()
		{
			try
			{
				var filename = "cache_keeper";

				if (!File.Exists(filename))
					return;

				using (var serializationStream = new FileStream("cache_keeper", FileMode.Open, FileAccess.Read))
				{
					BinaryFormatter formatter = new BinaryFormatter();

					var loadedCacheKeepr = (CacheKeeper)formatter.Deserialize(serializationStream);

					MaxCapacity = loadedCacheKeepr.MaxCapacity;
					MaxDownloadingTIme = loadedCacheKeepr.MaxDownloadingTIme;
					AllCache = loadedCacheKeepr.AllCache;

					serializationStream.Close();
				}
			}
			catch (Exception e)
			{

			}
		}

		#endregion

		#region Constructor

		public CacheKeeper()
		{
			MaxCapacity = int.Parse(ConfigurationManager.AppSettings["MaxCapacity"]);
			MaxDownloadingTIme = int.Parse(ConfigurationManager.AppSettings["MaxDownloadingTIme"]);
			AllCache = new List<KeyValuePair<long, NewPicture>>();

			Load();
		}

		#endregion
	}

	[Serializable()]
	public class NewPicture
	{
		#region Constructor

		public NewPicture(string path)
		{
			this.FilePath = path.ToString();

			FileSize = new FileInfo(FilePath).Length;
		}

		#endregion

		#region Properties

		public string FilePath { get; set; }
		public long FileSize { get; set; }

		#endregion

		#region Methods

		public override string ToString()
		{
			return FilePath.ToString();
		}

		#endregion
	}
}
