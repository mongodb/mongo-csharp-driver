using System;
using System.Globalization;
using System.Threading;
using FluentAssertions;
using MongoDB.Driver.Linq;
using Xunit;

namespace MongoDB.Driver.Tests.ExpressionFlattening
{
	public class ArrayIndexFormatterTests
	{
		[Fact]
		public void MinusOneShouldResultInDollarSign()
		{
			ArrayIndexFormatter.FormatArrayIndex(-1).Should().Be("$");
		}

		[Fact]
		public void ZeroOrGreaterShouldBeFormattedToStringRepresentationOfNumber()
		{
			ArrayIndexFormatter.FormatArrayIndex(0).Should().Be("0");
			ArrayIndexFormatter.FormatArrayIndex(1).Should().Be("1");
			ArrayIndexFormatter.FormatArrayIndex(99).Should().Be("99");
		}

		[Fact]
		public void ShouldBeCultureInsensitive()
		{
			var swedishCulture = CultureInfo.GetCultureInfo("sv-SE");

			Thread.CurrentThread.CurrentCulture = swedishCulture;
			Thread.CurrentThread.CurrentUICulture = swedishCulture;

			ArrayIndexFormatter.FormatArrayIndex(-1).Should().Be("$");
		}
	}
}