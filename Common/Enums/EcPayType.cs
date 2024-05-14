using System.ComponentModel;

namespace Common.Enums
{
    internal class EcPayType
    {
    }

    public enum CarrierType
    {
        [Description("")]
        None,

        [Description("1")]
        Member,

        [Description("2")]
        NaturalPersonEvidence,

        [Description("3")]
        PhoneBarcode
    }
}
