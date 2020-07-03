namespace EvtxReader.Converters
{
	using System;
	using System.Globalization;
	using System.Windows;
	using System.Windows.Data;
	using Constants;

	public class EventLevelToIconVisibilityConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			EventLevel level = (EventLevel)Enum.Parse(typeof(EventLevel), value.ToString());

			if (level.ToString() == parameter.ToString() ||
				(level == EventLevel.Critical && parameter.ToString() == "Error"))
			{
				return Visibility.Visible;
			}

			return Visibility.Collapsed;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
