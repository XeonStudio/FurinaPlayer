namespace SonicWave.Audio.Vst3;

internal struct ProcessData
{
	public int ProcessMode;

	public int SymbolicSampleSize;

	public int NumSamples;

	public int NumInputs;

	public int NumOutputs;

	public nint Inputs;

	public nint Outputs;

	public nint InputParameterChanges;

	public nint OutputParameterChanges;

	public nint InputEvents;

	public nint OutputEvents;

	public nint ProcessContext;
}
