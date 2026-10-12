namespace NeoLauncher.Extensions;

public static class ByteArrayExtension
{
	public static int Scan(this byte[] array, byte[] data, int offset = 0)
	{
		for (int i = offset; i <= array.Length - data.Length; i++)
		{
			bool flag = true;
			for (int j = 0; j < data.Length; j++)
			{
				if (array[i + j] != data[j])
				{
					flag = false;
					break;
				}
			}
			if (flag)
			{
				return i;
			}
		}
		return -1;
	}
}
