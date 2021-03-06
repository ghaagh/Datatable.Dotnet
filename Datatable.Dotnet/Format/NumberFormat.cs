
namespace Datatable.Dotnet.Format
{
    public class NumberFormat
    {
        private NumberFormat() { }
        public NumberFormat(char thousandSeparator, char decimalPoint, int maxDecimal)
        {
            ThousandSeparator = thousandSeparator;
            DecimalPoint = decimalPoint;
            MaxDecimal = maxDecimal;
        }
        public char ThousandSeparator { get; set; }
        public char DecimalPoint { get; set; }
        public int MaxDecimal { get; set; }
        public static NumberFormat DefaultInt()
        {
            return new NumberFormat(',','.',0); 
        }
        public static NumberFormat DefaultDecimal()
        {
            return new NumberFormat(',', '.', 2);
        }
    }
}
