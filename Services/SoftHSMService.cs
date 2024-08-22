using NBitcoin;
using Net.Pkcs11Interop.Common;
using Net.Pkcs11Interop.HighLevelAPI;
using ISession = Net.Pkcs11Interop.HighLevelAPI.ISession;

namespace SoftHSM_API_NET_8.Services
{
    public static class SoftHSMService
    {
        //Path to PKCS#11 lib from device vendor.
        internal static readonly string? Pkcs11LibraryPath = Environment.GetEnvironmentVariable("P11_LIB", EnvironmentVariableTarget.Process);
        //Factory to be used by developer and Pkcs11Interop library
        internal static readonly Pkcs11InteropFactories Factories = new();

        static SoftHSMService()
        {
            using (IPkcs11Library pkcs11Library = Factories.Pkcs11LibraryFactory.LoadPkcs11Library(Factories, Pkcs11LibraryPath, Helper.AppType))
            {
                InitCryptoToken(pkcs11Library);
                try
                {
                    InitMasterKeyObject(pkcs11Library);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }
        }
        public static string SignBTCTransfer(string? unsignedTxHash, string? keyPath, List<Coin> coinList)
        {
            using IPkcs11Library pkcs11Library = Factories.Pkcs11LibraryFactory.LoadPkcs11Library(Factories, Pkcs11LibraryPath, Helper.AppType);
            using ISession? session = Helper.GetApplicationSlot(pkcs11Library)?.OpenSession(SessionType.ReadWrite);
            if (session == null)
            {
                throw new Exception("Failed to open a session.");
            }

            try
            {

                if (!NBitcoin.KeyPath.TryParse(keyPath, out NBitcoin.KeyPath? path) || path == null)
                {
                    throw new Exception("Key path format is not valid!");
                }

                // Parse unsigned transaction and sign it.
                if (!Transaction.TryParse(unsignedTxHash, Helper.network, out Transaction? tx) || tx == null)
                {
                    throw new Exception("Failed to parse the transaction from the provided payload!");
                }

                // Derive key from given key path.
                ExtKey rootKey = ExtractMasterKey(session);
                var derivedKey = rootKey.Derive(path);
                tx.Sign(derivedKey.PrivateKey.GetBitcoinSecret(Network.TestNet), coinList);
                return tx.ToHex();
            }
            catch(Exception ex)
            {
                throw new Exception(ex.Message);
            }
            finally
            {
                // Ensure logout happens even if an exception is thrown
                session.Logout();
            }
        }

        public static ExtKey GetExtKey(string keyPath)
        {
            using IPkcs11Library pkcs11Library = Factories.Pkcs11LibraryFactory.LoadPkcs11Library(Factories, Pkcs11LibraryPath, Helper.AppType);
            using ISession? session = Helper.GetApplicationSlot(pkcs11Library)?.OpenSession(SessionType.ReadWrite);
            if (session == null) 
            {
                throw new Exception("Failed to open a session.");
            }

            try
            {

                if (!NBitcoin.KeyPath.TryParse(keyPath, out NBitcoin.KeyPath? path) || path == null)
                {
                    throw new Exception("Key path format is not valid!");
                }

                return ExtractMasterKey(session).Derive(path);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
            finally
            {
                // Ensure logout happens even if an exception is thrown
                session.Logout();
            }

        }
        
        // This function initializes a token in a SoftHSM slot with desired Token Label
        // configured in Helper class.
        private static void InitCryptoToken(IPkcs11Library pkcs11Library)
        {
            List<ISlot> slots = pkcs11Library.GetSlotList(SlotsType.WithOrWithoutTokenPresent);

            foreach (ISlot slot in slots)
            {
                try
                {
                    if (slot.GetTokenInfo().Label == Helper.ApplicationName)
                    {
                        Console.WriteLine($"{Helper.ApplicationName} Token exists.");
                        return; // No need to initialize
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error checking token info for slot {slot.SlotId}: {ex.Message}");
                    continue; // Continue checking other slots
                }
            }

            // If there isn't any token belong to the application, initialize with SOPin and Normal User Pin.
            foreach (ISlot slot in slots)
            {
                try
                {
                    if (!slot.GetTokenInfo().TokenFlags.TokenInitialized)
                    {
                        // Initialize token and SO (security officer) pin
                        slot.InitToken(Helper.SecurityOfficerPin, Helper.ApplicationName);

                        // Open read-write session
                        using (ISession session = slot.OpenSession(SessionType.ReadWrite))
                        {
                            // Login as SO (security officer)
                            session.Login(CKU.CKU_SO, Helper.SecurityOfficerPin);
                            // Initialize user pin
                            session.InitPin(Helper.NormalUserPin);
                            session.Logout();
                        }

                        Console.WriteLine($"{Helper.ApplicationName} Token initialized at:\nSlot {slot.SlotId}\nToken Serial Number: {slot.GetTokenInfo().SerialNumber}\n");
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error initializing token for slot {slot.SlotId}: {ex.Message}");
                    continue; // Continue checking other slots
                }
            }


        }
        private static void InitMasterKeyObject(IPkcs11Library pkcs11Library)
        {
            // Find the application slot
            ISlot? slot = Helper.GetApplicationSlot(pkcs11Library);
            if (slot == null)
            {
                throw new Exception($"{Helper.ApplicationName} token not found in existing slots to generate master key!");
            }

            // Open session and initialize master key
            using ISession session = slot.OpenSession(SessionType.ReadWrite);
            try
            {
                session.Login(CKU.CKU_USER, Helper.NormalUserPin);
                byte[] masterKey = CreateMasterKey();

                try
                {
                    StoreMasterKeyObject(session, masterKey);
                }
                finally
                {
                    // Clear the master key from memory after use
                    Array.Clear(masterKey, 0, masterKey.Length);
                }
            }
            finally
            {
                session.Logout();
            }

        }
        private static byte[] CreateMasterKey()
        {
            try
            {
                // Create mnemonic from a predefined set of words.
                Mnemonic mnemonic = new Mnemonic(Helper.mnemonic, Wordlist.English);

                // Derive master key object from mnemonic.
                ExtKey masterKey = mnemonic.DeriveExtKey();

                // Get the private key and chain code as byte arrays.
                byte[] privateKeyBytes = masterKey.PrivateKey.ToBytes();
                byte[] chainCodeBytes = masterKey.ChainCode;

                // Combine the private key and chain code into a single byte array.
                byte[] masterKeyBytes = new byte[privateKeyBytes.Length + chainCodeBytes.Length];
                privateKeyBytes.CopyTo(masterKeyBytes, 0);
                chainCodeBytes.CopyTo(masterKeyBytes, privateKeyBytes.Length);

                return masterKeyBytes;
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to create master key.", ex);
            }
        }
        private static void StoreMasterKeyObject(ISession session, byte[] masterKey)
        {
            // Define attributes for the master key object
            var masterKeyAttributes = new List<IObjectAttribute>()
            {
                session.Factories.ObjectAttributeFactory.Create(CKA.CKA_CLASS, CKO.CKO_DATA),
                session.Factories.ObjectAttributeFactory.Create(CKA.CKA_TOKEN, true),
                session.Factories.ObjectAttributeFactory.Create(CKA.CKA_PRIVATE, true),
                session.Factories.ObjectAttributeFactory.Create(CKA.CKA_MODIFIABLE, false),
                session.Factories.ObjectAttributeFactory.Create(CKA.CKA_LABEL, Helper.MasterKeyObjectLabel)
            };

            // Check if the master key already exists
            List<IObjectHandle> existingMasterKeys = session.FindAllObjects(masterKeyAttributes);
            if (existingMasterKeys.Count > 0)
            {
                Console.WriteLine("Master key already exists!");
                return;
            }

            // Add the master key value attribute
            masterKeyAttributes.Add(session.Factories.ObjectAttributeFactory.Create(CKA.CKA_VALUE, masterKey));

            try
            {
                // Create the master key object
                session.CreateObject(masterKeyAttributes);
                Console.WriteLine("Master key object creation is successful.");
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to create the master key object." + ex);
            }
        }
        private static ExtKey ExtractMasterKey(ISession session)
        {
            try
            {
                // Login to read private object (necessary).
                session.Login(CKU.CKU_USER, Helper.NormalUserPin);

                // Define master key search parameters.
                var searchAttributes = new List<IObjectAttribute>()
                {
                    session.Factories.ObjectAttributeFactory.Create(CKA.CKA_CLASS, CKO.CKO_DATA),
                    session.Factories.ObjectAttributeFactory.Create(CKA.CKA_TOKEN, true),
                    session.Factories.ObjectAttributeFactory.Create(CKA.CKA_PRIVATE, true),
                    session.Factories.ObjectAttributeFactory.Create(CKA.CKA_MODIFIABLE, false),
                    session.Factories.ObjectAttributeFactory.Create(CKA.CKA_LABEL, Helper.MasterKeyObjectLabel)
                };

                // Get master key object handle.
                List<IObjectHandle> keyHandles = session.FindAllObjects(searchAttributes);

                Console.WriteLine(keyHandles.Count);

                // If master key exists, extract it.
                if (keyHandles.Count > 0)
                {
                    var keyAttributes = session.GetAttributeValue(keyHandles[0], new List<CKA>() { CKA.CKA_VALUE });
                    byte[] masterKeyBytes = keyAttributes[0].GetValueAsByteArray();

                    // Split master key into private key and chain code.
                    byte[] privateKeyBytes = new ArraySegment<byte>(masterKeyBytes, 0, 32).ToArray();
                    byte[] chainCodeBytes = new ArraySegment<byte>(masterKeyBytes, 32, 32).ToArray();
                    Key privateKey = new Key(privateKeyBytes);
                    ExtKey masterKey = new ExtKey(privateKey, chainCodeBytes);

                    // Clear sensitive data from memory.
                    Array.Clear(masterKeyBytes, 0, masterKeyBytes.Length);
                    Array.Clear(privateKeyBytes, 0, privateKeyBytes.Length);
                    Array.Clear(chainCodeBytes, 0, chainCodeBytes.Length);

                    return masterKey;
                }

                // If key not found, throw exception.
                throw new Exception("Master key not found in token!");
            }
            finally
            {
                // Ensure logout happens even if an exception is thrown.
                session.Logout();
            }
        }

    }
}