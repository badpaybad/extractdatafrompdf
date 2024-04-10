using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PdfExtractor.Domains
{
    public static class StringCipher
    {

        public readonly static DateTime unixBeginDateTime = new DateTime(1970, 1, 1, 0, 0, 0);

        static string[] _specialMacAddr = { " ", "/", "-", ":", "\\" };

        public static string NormalizeMacAddress(this string macAddress)
        {
            foreach (var p in _specialMacAddr)
            {
                macAddress = macAddress.Replace(p, "");
            }

            return macAddress;
        }

        public static long ToUnixTimestamp(this DateTime d)
        {
            var epoch = d - unixBeginDateTime;

            return (long)epoch.TotalSeconds;
        }

        public static TimeSpan ToUnixTimeSpan(this DateTime d)
        {
            var epoch = d - unixBeginDateTime;

            return new TimeSpan(epoch.Ticks);
        }

        public static DateTime FromUnixTime(this long unixDateTime)
        {
            return DateTimeOffset.FromUnixTimeSeconds(unixDateTime).DateTime.ToLocalTime();
        }
        public static double ToDoubleTimestamp(this DateTime d)
        {
            var epoch = d - unixBeginDateTime;

            return epoch.TotalSeconds;
        }

        public static DateTime FromDoubleTime(this double unixDateTime)
        {
            return DateTimeOffset.FromUnixTimeSeconds((long)unixDateTime).DateTime.ToLocalTime();
        }
        public static string ToArrayHexString(byte[] ba)
        {
            return string.Join(",", BitConverter.ToString(ba).Split('-').Select(i => "0x" + i));
        }
        public static byte[] HexStringToArray(string hexString)
        {
            var arr = hexString.Trim().Split(',');
            return arr.Select(i => Convert.ToByte(i.Trim().Substring(2), 16)).ToArray();
        }

        public static void AesGenreate(string keyOrPwd, out byte[] aesKey, out byte[] aesIv)
        {
            HashAlgorithm hash = MD5.Create();

            aesKey = hash.ComputeHash(Encoding.Unicode.GetBytes(keyOrPwd));

            var aesAlg = Aes.Create();
            aesAlg.Mode = CipherMode.CBC;
            aesAlg.Padding = PaddingMode.Zeros;
            aesAlg.Key = aesKey;

            aesAlg.GenerateIV();
            aesIv = aesAlg.IV;

        }
        public static void AesGenreate(string keyOrPwd, out string arrayHexStringKey, out string arrayHexStringIv)
        {
            AesGenreate(keyOrPwd, out byte[] key, out byte[] iv);
            arrayHexStringIv = ToArrayHexString(iv);
            arrayHexStringKey = ToArrayHexString(key);
        }
        public static string AesEncrypt(string plainText, string arrayHexStringKey, string arrayHexStringIv)
        {
            var key = HexStringToArray(arrayHexStringKey);
            var ivector = HexStringToArray(arrayHexStringIv);

            return AesEncrypt(plainText, key, ivector);
        }
        public static string AesEncrypt(string plainText, byte[] key, byte[] iv)
        {
            var b64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(plainText));

            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = key;
                aesAlg.IV = iv;
                aesAlg.Mode = CipherMode.CBC;
                aesAlg.Padding = PaddingMode.Zeros;

                // Create an encryptor to perform the stream transform.
                ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                // Create the streams used for encryption.
                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                        {
                            //Write all data to the stream.
                            swEncrypt.Write(b64);
                        }
                        return Convert.ToBase64String(msEncrypt.ToArray());
                    }
                }
            }
        }

        public static string AesDecrypt(string encryptedInBase64, string arrayHexStringKey, string arrayHexStringIv)
        {
            var key = HexStringToArray(arrayHexStringKey);
            var ivec = HexStringToArray(arrayHexStringIv);
            return AesDecrypt(encryptedInBase64, key, ivec);
        }
        public static string AesDecrypt(string encryptedInBase64, byte[] key, byte[] iv)
        {
            var cipherText = Convert.FromBase64String(encryptedInBase64);

            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = key;
                aesAlg.IV = iv;
                aesAlg.Mode = CipherMode.CBC;
                aesAlg.Padding = PaddingMode.Zeros;

                // Create a decryptor to perform the stream transform.
                ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                // Create the streams used for decryption.
                using (MemoryStream msDecrypt = new MemoryStream(cipherText))
                {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                        {

                            // Read the decrypted bytes from the decrypting stream
                            // and place them in a string.
                            string s = StripAdnormal(srDecrypt.ReadToEnd());
                            byte[] bytes = Convert.FromBase64String(s);
                            return Encoding.UTF8.GetString(bytes);
                        }
                    }
                }
            }
        }

        public static string StripAdnormal(string base64)
        {
            var idx = base64.LastIndexOf("\0\0\0");
            if (idx <= 0) return base64;
            return base64.Substring(0, idx);
        }

        public static bool RsaGenerate(out string privateKeyInBase64, out string publicKeyInBase64)
        {
            privateKeyInBase64 = string.Empty;
            publicKeyInBase64 = string.Empty;
            try
            {
                RSA rsa = RSA.Create(2048);
                privateKeyInBase64 = Convert.ToBase64String(rsa.ExportRSAPrivateKey());

                publicKeyInBase64 = Convert.ToBase64String(rsa.ExportRSAPublicKey());
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static string RsaEncrypt(string data, string publicKeyInBase64)
        {
            RSA rsa = RSA.Create();
            rsa.ImportRSAPublicKey(Convert.FromBase64String(publicKeyInBase64), out int readByte);

            byte[] datainbyte = Encoding.UTF8.GetBytes(data);
            byte[] ecrypedinbyte = rsa.Encrypt(datainbyte, RSAEncryptionPadding.OaepSHA256);
            return Convert.ToBase64String(ecrypedinbyte);
        }
        public static string RsaDecrypt(string dataEncryptedInBase64, string privateKeyInBase64)
        {
            RSA rsa = RSA.Create();
            rsa.ImportRSAPrivateKey(Convert.FromBase64String(privateKeyInBase64), out int readByte);

            return Encoding.UTF8.GetString(rsa.Decrypt(Convert.FromBase64String(dataEncryptedInBase64), RSAEncryptionPadding.Pkcs1));
        }

        public static string RsaGetSign(string data, string privateKeyInBase64)
        {
            RSA rsa = RSA.Create();
            rsa.ImportRSAPrivateKey(Convert.FromBase64String(privateKeyInBase64), out int readByte);

            return Convert.ToBase64String(rsa.SignData(Encoding.UTF8.GetBytes(data), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));
        }

        public static bool RsaVerify(string data, string signInBase64, string publicKeyInBase64)
        {
            RSA rsa = RSA.Create();
            rsa.ImportRSAPublicKey(Convert.FromBase64String(publicKeyInBase64), out int readByte);

            return rsa.VerifyData(Encoding.UTF8.GetBytes(data), Convert.FromBase64String(signInBase64), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        }
        public static string GetMd5Hash(this string input)
        {
            using (var md5Hash = MD5.Create())
            {
                // Convert the input string to a byte array and compute the hash. 
                byte[] data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(input));

                // Create a new Stringbuilder to collect the bytes 
                // and create a string.
                var sBuilder = new StringBuilder();

                // Loop through each byte of the hashed data  
                // and format each one as a hexadecimal string. 
                for (var i = 0; i < data.Length; i++)
                {
                    sBuilder.Append(data[i].ToString("x2"));
                }
                return sBuilder.ToString();
            }
        }

        public static string GetSha256Hash(this string input)
        {
            byte[] data = Encoding.UTF8.GetBytes(input);
            using (SHA256 sha = System.Security.Cryptography.SHA256.Create())
            {
                return Convert.ToBase64String(sha.ComputeHash(data));
            }
        }

        public static string GenerateToken(string salt = "")
        {

            using (var sha = SHA512.Create())
            {
                return System.Convert.ToBase64String(sha.ComputeHash(UTF8Encoding.UTF8.GetBytes($"{salt}.{Guid.NewGuid()}")));
            }
        }

        public static string HashPassword(string src)
        {
            using (var sha = SHA512.Create())
            {
                return Convert.ToBase64String(sha.ComputeHash(UTF8Encoding.UTF8.GetBytes(src)));
            }
        }

        public static string StripHTML(this string input)
        {
            return Regex.Replace(input, "<.*?>", " ");
        }

        static Random _random = new Random();
        private static string _alphabet = "qwertyuiopasdfghjklzxcvbnm";
        private static string _number = "1234567890";
        private static string _special = "~!@#$%^&*";
        public static string RandomPassword(short lenghtOfAlphabet = 3, short lenghtOfUpperCase = 1, short lenghtOfNumber = 2, short lenghtOfSpecial = 0)
        {
            var alphabet = new List<char>();
            for (var i = 0; i < lenghtOfAlphabet; i++)
            {
                alphabet.Add(_alphabet[_random.Next(0, _alphabet.Length)]);
            }
            for (var i = 0; i < lenghtOfUpperCase; i++)
            {
                var irnd = _random.Next(0, alphabet.Count);
                alphabet[irnd] = alphabet[irnd].ToString().ToUpper()[0];
            }

            var number = new List<char>();
            for (var i = 0; i < lenghtOfNumber; i++)
            {
                number.Add(_number[_random.Next(0, _number.Length)]);
            }
            var special = new List<char>();
            for (var i = 0; i < lenghtOfSpecial; i++)
            {
                special.Add(_special[_random.Next(0, _special.Length)]);
            }

            var temp = new List<char>();
            temp.AddRange(alphabet);
            temp.AddRange(number);
            temp.AddRange(special);

            temp = temp.Shuffle().ToList();

            return string.Join("", temp);
        }
        public static IList<T> Shuffle<T>(this IList<T> ts)
        {
            return ts.OrderBy(a => Guid.NewGuid()).ToList();
        }

        // Create secret IV
        static readonly byte[] iv = new byte[16] { 0x2, 0x1, 0x1, 0x2, 0x8, 0x4, 0x0, 0x0, 0x0, 0x0, 0x4, 0x8, 0x2, 0x1, 0x1, 0x2 };

        public static bool StrEncript(this string plainText, string password, out string encrypted)
        {
            encrypted = plainText;
            try
            {
                // Create sha256 hash
                SHA256 mySHA256 = SHA256.Create();
                byte[] key = mySHA256.ComputeHash(Encoding.UTF8.GetBytes(password));
                encrypted = plainText.EncryptAes256CbcString(key, iv);

                return true;
            }
            catch
            {
                return false;
            }
        }
        public static bool StrDecript(this string encrypted, string password, out string decrypted)
        {
            decrypted = encrypted;
            try
            {
                // Create sha256 hash
                SHA256 mySHA256 = SHA256.Create();
                byte[] key = mySHA256.ComputeHash(Encoding.UTF8.GetBytes(password));
                decrypted = encrypted.DecryptAes256CbcString(key, iv);

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("StrDecript");
                Console.WriteLine(ex);
                return false;
            }
        }
        public static string EncryptAes256CbcString(this string plainText, byte[] key, byte[] iv)
        {
            // Instantiate a new Aes object to perform string symmetric encryption
            using (Aes encryptor = Aes.Create())
            {
                encryptor.Mode = CipherMode.CBC;

                // Set key and IV
                byte[] aesKey = new byte[32];
                Array.Copy(key, 0, aesKey, 0, 32);
                encryptor.Key = aesKey;
                encryptor.IV = iv;

                // Instantiate a new MemoryStream object to contain the encrypted bytes
                MemoryStream memoryStream = new MemoryStream();

                // Instantiate a new encryptor from our Aes object
                ICryptoTransform aesEncryptor = encryptor.CreateEncryptor();

                // Instantiate a new CryptoStream object to process the data and write it to the 
                // memory stream
                CryptoStream cryptoStream = new CryptoStream(memoryStream, aesEncryptor, CryptoStreamMode.Write);

                // Convert the plainText string into a byte array
                byte[] plainBytes = Encoding.ASCII.GetBytes(plainText);

                // Encrypt the input plaintext string
                cryptoStream.Write(plainBytes, 0, plainBytes.Length);

                // Complete the encryption process
                cryptoStream.FlushFinalBlock();

                // Convert the encrypted data from a MemoryStream to a byte array
                byte[] cipherBytes = memoryStream.ToArray();

                // Close both the MemoryStream and the CryptoStream
                memoryStream.Close();
                cryptoStream.Close();

                // Convert the encrypted byte array to a base64 encoded string
                string cipherText = Convert.ToBase64String(cipherBytes, 0, cipherBytes.Length);

                // Return the encrypted data as a string
                return cipherText;
            }
        }

        public static string DecryptAes256CbcString(this string cipherTextInBase64String, byte[] key, byte[] iv)
        {
            // Instantiate a new Aes object to perform string symmetric encryption
            using (Aes encryptor = Aes.Create())
            {
                encryptor.Mode = CipherMode.CBC;

                // Set key and IV
                byte[] aesKey = new byte[32];
                Array.Copy(key, 0, aesKey, 0, 32);
                encryptor.Key = aesKey;
                encryptor.IV = iv;

                // Instantiate a new MemoryStream object to contain the encrypted bytes
                MemoryStream memoryStream = new MemoryStream();

                // Instantiate a new encryptor from our Aes object
                ICryptoTransform aesDecryptor = encryptor.CreateDecryptor();

                // Instantiate a new CryptoStream object to process the data and write it to the 
                // memory stream
                CryptoStream cryptoStream = new CryptoStream(memoryStream, aesDecryptor, CryptoStreamMode.Write);

                // Will contain decrypted plaintext
                string plainText = String.Empty;

                try
                {
                    // Convert the ciphertext string into a byte array
                    byte[] cipherBytes = Convert.FromBase64String(cipherTextInBase64String);

                    // Decrypt the input ciphertext string
                    cryptoStream.Write(cipherBytes, 0, cipherBytes.Length);

                    // Complete the decryption process
                    cryptoStream.FlushFinalBlock();

                    // Convert the decrypted data from a MemoryStream to a byte array
                    byte[] plainBytes = memoryStream.ToArray();

                    // Convert the decrypted byte array to string
                    plainText = Encoding.ASCII.GetString(plainBytes, 0, plainBytes.Length);
                }
                finally
                {
                    // Close both the MemoryStream and the CryptoStream
                    memoryStream.Close();
                    cryptoStream.Close();
                }

                // Return the decrypted data as a string
                return plainText;
            }

        }

        private static string KillSign(string needReplace, string signUnicode, string replaceChar)
        {
            var tempNr = needReplace;
            for (int i = 0; i < signUnicode.Length; i++)
            {
                tempNr = tempNr.Replace(signUnicode[i].ToString(), replaceChar);
            }
            return tempNr;
        }

        public static string KillSign(this string strUnicode)
        {
            strUnicode = strUnicode.ToLower();
            string a = "âăáàảãạấầẩẫậắằẳẵặ";
            string o = "ôơóòỏõọốồổỗộớờởỡợ";
            string e = "eéèẻẹẽêếềệểễ";
            string i = "iíìỉịĩ";
            string y = "yýỳỷỵỹ";
            string u = "uưúùủũụứừửữự";
            string d = "đ";

            int l = a.Length + o.Length + e.Length + i.Length + y.Length + u.Length + d.Length;
            if (strUnicode.Length > l * 3)
            {

                strUnicode = KillSign(strUnicode, a, "a");
                strUnicode = KillSign(strUnicode, o, "o");
                strUnicode = KillSign(strUnicode, e, "e");
                strUnicode = KillSign(strUnicode, i, "i");
                strUnicode = KillSign(strUnicode, y, "y");
                strUnicode = KillSign(strUnicode, u, "u");
            }
            else
            {
                int ll = strUnicode.Length;
                for (int ii = 0; ii < ll; ii++)
                {
                    string temp = strUnicode.Substring(ii, 1);

                    if (a.IndexOf(temp) >= 0)
                    {
                        strUnicode = strUnicode.Replace(temp, "a");
                    }
                    if (e.IndexOf(temp) >= 0)
                    {
                        strUnicode = strUnicode.Replace(temp, "e");
                    }
                    if (o.IndexOf(temp) >= 0)
                    {
                        strUnicode = strUnicode.Replace(temp, "o");
                    }
                    if (i.IndexOf(temp) >= 0)
                    {
                        strUnicode = strUnicode.Replace(temp, "i");
                    }
                    if (y.IndexOf(temp) >= 0)
                    {
                        strUnicode = strUnicode.Replace(temp, "y");
                    }
                    if (u.IndexOf(temp) >= 0)
                    {
                        strUnicode = strUnicode.Replace(temp, "u");
                    }
                }
            }

            return strUnicode.Replace("đ", "d");

        }
        public static string ReplaceAll(this string src, string oldStr, string newStr)
        {
            while (true)
            {
                var idx = src.IndexOf(oldStr);
                if (idx < 0) break;

                src = src.Replace(oldStr, newStr);
            }
            return src;
        }

    }

}
