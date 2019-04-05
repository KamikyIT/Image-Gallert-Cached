using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace CashPictures
{
	public class NonVisibleConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			var visibility = value is Visibility ? (Visibility) value : Visibility.Visible;

			switch (visibility)
			{
				case Visibility.Visible:
					return Visibility.Hidden;
				case Visibility.Hidden:
					return Visibility.Visible;
				case Visibility.Collapsed:
					return Visibility.Visible;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}