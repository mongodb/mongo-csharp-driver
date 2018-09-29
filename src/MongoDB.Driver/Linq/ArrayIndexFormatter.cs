namespace MongoDB.Driver.Linq
{
	/// <summary>
	/// In order to support the positional operator, the number -1 should be treated specially when used as an array index.
	/// </summary>
	public class ArrayIndexFormatter
	{
		/// <summary>
		/// Formats any object to its string representation, unless <paramref name="index"/> is of type <c>int</c>, in which case a dollar sign ($) is returned.
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public static string FormatArrayIndex(object index)
		{
			// We've treated -1 as meaning $ operator. We can't break this now,
			// so, specifically when we are flattening fields names, this is 
			// how we'll continue to treat -1.

			return index is int && (int)index == -1
				? "$"
				: index.ToString();
		}
	}
}