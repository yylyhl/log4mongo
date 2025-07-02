using System.Text.RegularExpressions;

namespace Log4Mongo
{
	public class UnitResolver
	{
		public static long Resolve(string valueWithUnit)
		{
			if(valueWithUnit == null)
			{
				return 0;
			}

            if (!int.TryParse(valueWithUnit, out int result))
            {
                var regex = new Regex(@"^(\d+)(k|MB){0,1}$");
                var match = regex.Match(valueWithUnit);

                if (match.Success)
                {
                    var value = int.Parse(match.Groups[1].Value);
                    var multiplier = GetMultiplier(match.Groups[2].Value);
                    result = value * multiplier;
                }
            }

            return result;
		}

		private static int GetMultiplier(string unit)
		{
			switch (unit)
			{
				case "k":
					return 1000;
				case "MB":
					return 1024 * 1024;
				default:
					return 0;
			}
		}
	}
}