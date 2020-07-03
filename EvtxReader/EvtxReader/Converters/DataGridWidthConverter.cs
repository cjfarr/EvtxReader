namespace EvtxReader.Converters
{
	using System;
	using System.Globalization;
	using System.Windows.Data;

	public class DataGridWidthConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			double actualWidth = double.Parse(value.ToString());

			return actualWidth - 20d;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
