using System;

namespace SonicWave.Audio.Vst3;

internal static class Vst3Ids
{
	public static readonly Guid FUnknown = new Guid("00000000-0000-0000-C000-000000000046");

	public static readonly Guid IPluginFactory = new Guid("7A4D811C-5211-4A1F-AED9-D2EE0B43BF9F");

	public static readonly Guid IPluginFactory2 = new Guid("0007B650-F24B-4C0B-A464-EDB9F00B2ABB");

	public static readonly Guid IPluginBase = new Guid("22888DDB-156E-45AE-8358-B34808190625");

	public static readonly Guid IComponent = new Guid("E831FF31-F2D5-4301-928E-BBEE25697802");

	public static readonly Guid IAudioProcessor = new Guid("42043F99-B7DA-453C-A569-E79D9AAEC33D");

	public static readonly Guid IEditController = new Guid("DCD7BBE3-7742-448D-A874-AACC979C759E");

	public static readonly Guid IPlugView = new Guid("5BC32507-D060-49EA-A615-1B522B755B29");

	public static readonly Guid IPlugFrame = new Guid("367FAF01-AFA9-4693-8D4D-A2A0ED0882A3");

	public static readonly Guid IHostApplication = new Guid("58E595CC-DB2D-4969-8B6A-AF8C36A664E5");

	public static readonly Guid IComponentHandler = new Guid("93A0BEA3-0BD0-45DB-8E89-0B0CC1E46AC6");

	public static readonly Guid IMessage = new Guid("936F033B-C6C0-47DB-BB08-82F813C1E613");

	public static readonly Guid IAttributeList = new Guid("1E5F0AEB-CC7F-4533-A254-401138AD5EE4");

	public static readonly Guid IConnectionPoint = new Guid("70A4156F-6E6E-4026-9891-48BFAA60D8D1");

	public static readonly Guid IParameterChanges = new Guid("A4779663-0BB6-4A56-B443-84A8466FEB9D");

	public static readonly Guid IParamValueQueue = new Guid("01263A18-ED07-4F6F-98C9-D3564686F9BA");

	public static readonly Guid IBStream = new Guid("C3BF6EA2-3099-4752-9B6B-F9901EE33E9B");
}
