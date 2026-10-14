using System.Numerics;
using System.Text;
using System.Text.Json;
using EnsembleRoot.Saving;
using EnsembleRoot.Saving.SerDes;
using Xunit;

namespace EnsembleRoot.Tests;

public sealed class JsonSaveSerializerTests
{
	private static readonly JsonSaveSerializer Serializer = new();

	[Theory]
	[InlineData("""{"Instances":null}""")]
	[InlineData("""{"Instances":[null]}""")]
	[InlineData("""{"Instances":[{"AssetId":1,"Position":[0,0,0]}]}""")]
	[InlineData("""{"Instances":[{"AssetId":1,"Rotation":[0,0,0,1],"Properties":{"a":1,"A":2}}]}""")]
	[InlineData("""{"Instances":[{"AssetId":1,"Rotation":[0,0,0,1],"Properties":{"a":1e400}}]}""")]
	public void Deserialize_InvalidSave_ThrowsInvalidData(string json) =>
		Assert.Throws<InvalidDataException>(() => Deserialize(json));

	[Fact]
	public void Deserialize_OverflowingRotation_Throws() =>
		Assert.Throws<JsonException>(static () =>
			Deserialize("""{"Instances":[{"Rotation":[3e38,3e38,3e38,3e38]}]}"""));

	[Fact]
	public void Deserialize_UnnormalizedRotation_IsNormalizedLikeBinary()
	{
		var instance = Deserialize("""{"Instances":[{"Rotation":[0,0,0,2]}]}""").Instances[0];
		Assert.Equal(Quaternion.Identity, instance.Rotation);
	}

	private static CreationSaveData Deserialize(string json) =>
		Serializer.Deserialize(new MemoryStream(Encoding.UTF8.GetBytes(json)));
}
