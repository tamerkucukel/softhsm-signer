using NBitcoin;
using Net.Pkcs11Interop.Common;
using Net.Pkcs11Interop.HighLevelAPI;

namespace SoftHSM_API_NET_8.Services
{
    internal static class Helper
    {
        public static AppType AppType = AppType.MultiThreaded;

        public static string ApplicationName = "cryptographic_signature";

        public static string SecurityOfficerPin = "1234";

        public static string NormalUserPin = "1234";

        public static string mnemonic = "clap angry pass cheap acquire try coral exist bargain asset harsh uphold clump disagree shell guide festival chair gain lounge vivid life find cake";

        public static string MasterKeyObjectLabel = "Master-Key";

        public static Network network = Network.TestNet;
        

        public static ISlot? GetApplicationSlot(IPkcs11Library pkcs11Library)
        {
            try
            {
                // Retrieve the list of slots
                List<ISlot> slots = pkcs11Library.GetSlotList(SlotsType.WithOrWithoutTokenPresent);

                // Iterate through the slots to find the one matching the application name
                foreach (ISlot slot in slots)
                {
                    try
                    {
                        if (slot.GetTokenInfo().Label == ApplicationName)
                        {
                            return slot;
                        }
                    }
                    catch (Exception ex)
                    {
                        throw new Exception($"Error retrieving token info for slot {slot.SlotId}: {ex.Message}");
                    }
                }

                // Token not found.
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving slot list: {ex.Message}");
                return null;
            }
        }
    }
}
