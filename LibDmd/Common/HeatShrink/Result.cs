namespace LibDmd.Common.HeatShrink
{
	public class Result
	{

		public int Count;

		public Code Code;

		public Result(int count, Code res)
		{
			Count = count;
			Code = res;
		}

		public bool IsError()
		{
			return Code == Code.ErrorNull || Code == Code.ErrorMisuse || Code == Code.ErrorUnknown;
		}

		public static Result res()
		{
			return new Result(0, Code.ErrorNull);
		}

		public static Result res(int count, Code res)
		{
			return new Result(count, res);
		}

		public static Result res(Code res)
		{
			return new Result(0, res);
		}

		public override string ToString()
		{
			return $"Result [count={Count}, code={Code}]";
		}
	}

	public enum Code
	{
		/// <summary>
		/// data sunk into input buffer
		/// </summary>
		Ok,
		/// <summary>
		/// data sunk into input buffer
		/// </summary>
		Full,
		/// <summary>
		/// input exhausted
		/// </summary>
		Empty,
		/// <summary>
		/// poll again for more output
		/// </summary>
		More,
		/// <summary>
		/// encoding is complete
		/// </summary>
		Done,
		/// <summary>
		/// NULL argument
		/// </summary>
		ErrorNull,
		ErrorMisuse,
		ErrorUnknown
	}
}
