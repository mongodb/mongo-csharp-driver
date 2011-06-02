namespace MongoDB.Bson
{
	public abstract class CustomBsonTypeMapper
	{
		public abstract BsonValue Convert(object source);
	}

	public abstract class CustomBsonTypeMapper<T> : CustomBsonTypeMapper
	{
		public override BsonValue Convert(object source)
		{
			return Convert((T) source);
		}

		public abstract BsonValue Convert(T source);
	}
}