using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;

namespace CashPictures
{
	public class MainWindowViewModel : INotifyPropertyChanged
	{
		#region Constructor

		public MainWindowViewModel()
		{
			CloseCommand = new RelayCommand(
				(window) =>
				{
					(window as Window).Close();
					_cacheManager.Save();
				},
				(window) => window != null && (window is Window)
			);

			OpenSetFolderCommand = new RelayCommand(
				(o) =>
				{
					var fbd = new FolderBrowserDialog();

					if (fbd.ShowDialog() != DialogResult.OK)
						return;

					FolderPath = fbd.SelectedPath;
				});

			GoLeftCommand = new RelayCommand(
				(o) =>
				{
					DisplayingImageIndex--;
				},
				(o) =>
				{
					return _files != null && DisplayingImageIndex > 0 && _files.Length > 0;
				});

			GoRightCommand = new RelayCommand(
				(o) =>
				{
					DisplayingImageIndex++;
				},
				(o) =>
				{
					return _files != null && DisplayingImageIndex < _files.Length - 1 && _files.Length > 0;
				});


			FolderPath = "Выберите папку...";

			_cacheManager = new CacheManager(new RandomAwaiter(), new CacheKeeper());

			ImagePath = _cacheManager.DefaultPicture.FilePath;
		}

		#endregion

		#region Private fields

		private string _folderPath;
		private int _displayingImageIndex;
		private string[] _files;
		private readonly string[] _imageExtensions = { ".jpeg", ".png", ".jpg" };
		private string _imagePath;
		private ICacheManager _cacheManager;
		private string _currentFileFromAll;
		private Task<NewPicture> _generatingImageTask;
		private Visibility _loadingImage;

		#endregion

		#region Commands

		public ICommand CloseCommand { get; private set; }
		public ICommand OpenSetFolderCommand { get; private set; }
		public ICommand GoLeftCommand { get; private set; }
		public ICommand GoRightCommand { get; private set; }

		#endregion

		#region Methods

		public int DisplayingImageIndex
		{
			get
			{
				return _displayingImageIndex;
			}
			private set
			{
				_displayingImageIndex = value;

				if (_displayingImageIndex == -1)
					DisplayDefaultImage();

				if (_files != null && _files.Any())
				{
					var file = _files[_displayingImageIndex];

					ImagePath = file;
				}

				NotifyPropertyChanged("DisplayingImageIndex");
			}
		}

		private void DisplayDefaultImage()
		{
			ImagePath = _cacheManager.DefaultPicture.FilePath;
		}

		private void StartDisplayFirstImage()
		{
			_files = Directory.GetFiles(FolderPath).Where(x => _imageExtensions.Contains(Path.GetExtension(x))).ToArray();

			DisplayingImageIndex = !_files.Any() ? -1 : 0;
		}

		#endregion

		#region Properties

		public string ImagePath
		{
			get { return _imagePath; }
			set
			{
				if (_imagePath == value)
					return;

				LoadingImage = Visibility.Visible;

				CurrentFileFromAll = _files == null || !_files.Any() ? "default" : _files[DisplayingImageIndex];
				
				_generatingImageTask = _cacheManager.GeneratePicture(value);

				_generatingImageTask.ContinueWith(task =>
				{
					_imagePath = task.Result.FilePath;

					NotifyPropertyChanged("ImagePath");

					LoadingImage = Visibility.Hidden;
				});
			}
		}

		public string CurrentFileFromAll
		{
			get { return _currentFileFromAll; }
			set
			{
				if (_currentFileFromAll == value)
					return;

				_currentFileFromAll = value;

				NotifyPropertyChanged("CurrentFileFromAll");
			}
		}

		public string FolderPath
		{
			get
			{
				return _folderPath;
			}
			set
			{
				if (_folderPath == value)
					return;

				_folderPath = value;

				if (Directory.Exists(_folderPath))
					StartDisplayFirstImage();

				NotifyPropertyChanged("FolderPath");
			}
		}

		public Visibility LoadingImage
		{
			get { return _loadingImage; }
			set
			{
				if (_loadingImage == value)
					return;

				_loadingImage = value;

				NotifyPropertyChanged("LoadingImage");
			}
		}

		#endregion

		#region INotifyPropertyChanged

		public event PropertyChangedEventHandler PropertyChanged;

		protected void NotifyPropertyChanged(String info)
		{
			if (PropertyChanged != null)
			{
				PropertyChanged(this, new PropertyChangedEventArgs(info));
			}
		}

		#endregion
	}
}
