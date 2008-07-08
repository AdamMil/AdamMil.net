using AdamMil.Security.PGP;

namespace AdamMil.Security.UI
{

public static class PGPUI
{
  public static string[] GetDefaultKeyServers()
  {
    return new string[]
    {
      "hkp://keyserver.mine.nu", "hkp://pgp.mit.edu", "hkp://wwwkeys.pgp.net", "ldap://certserver.pgp.com"
    };
  }

  public static string GetKeyName(PrimaryKey key)
  {
    return key.PrimaryUserId.Name + " (0x" + key.ShortKeyId + ")";
  }

  public static string GetKeyValidityDescription(Key key)
  {
    return key.Revoked ? "revoked" : key.Expired ? "expired" : PGPUI.GetTrustDescription(key.CalculatedTrust);
  }

  public static string GetSignatureDescription(OpenPGPSignatureType type)
  {
    switch(type)
    {
      case OpenPGPSignatureType.CanonicalBinary: case OpenPGPSignatureType.CanonicalText:
        return "data";
      case OpenPGPSignatureType.CasualCertification:
        return "casual certification";
      case OpenPGPSignatureType.CertificateRevocation:
        return "certificate revocation";
      case OpenPGPSignatureType.ConfirmationSignature:
        return "confirmation";
      case OpenPGPSignatureType.DirectKeySignature:
        return "data binding";
      case OpenPGPSignatureType.GenericCertification:
        return "certification";
      case OpenPGPSignatureType.PersonaCertification:
        return "unvalidated certification";
      case OpenPGPSignatureType.PositiveCertification:
        return "full certification";
      case OpenPGPSignatureType.PrimaryKeyBinding: case OpenPGPSignatureType.SubkeyBinding:
        return "key binding";
      case OpenPGPSignatureType.PrimaryKeyRevocation:
        return "key revocation";
      case OpenPGPSignatureType.Standalone:
        return "standalone";
      case OpenPGPSignatureType.SubkeyRevocation:
        return "subkey revocation";
      case OpenPGPSignatureType.TimestampSignature:
        return "timestamp";
      default:
        return "unknown";
    }
  }

  public static string GetTrustDescription(TrustLevel level)
  {
    switch(level)
    {
      case TrustLevel.Full:     return "full";
      case TrustLevel.Marginal: return "marginal";
      case TrustLevel.Never:    return "none";
      case TrustLevel.Ultimate: return "ultimate";
      default:                  return "unknown";
    }
  }
}

} // namespace AdamMil.Security.UI