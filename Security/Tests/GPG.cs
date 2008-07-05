using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using NUnit.Framework;
using AdamMil.Security.PGP;
using AdamMil.Security.PGP.GPG;
using AdamMil.Tests;

namespace AdamMil.Security.Tests
{

[TestFixture]
public class GPGTest : IDisposable
{
  ~GPGTest() { Dispose(true); }

  [TestFixtureTearDown]
  public void Dispose()
  {
    GC.SuppressFinalize(this);
    Dispose(false);
  }

  void Dispose(bool finalizing)
  {
    if(keyring != null)
    {
      File.Delete(keyring.PublicFile);
      File.Delete(keyring.SecretFile);
      File.Delete(keyring.TrustDbFile);
    }
  }

  const int Encrypter=0, Signer=1, Receiver=2; // keys that were imported

  [TestFixtureSetUp]
  public void Setup()
  {
    // we'll use "aoeu" as the password in places where passwords are needed
    unsafe
    {
      char* pass = stackalloc char[4];
      pass[0] = 'a';
      pass[1] = 'o';
      pass[2] = 'e';
      pass[3] = 'u';
      password = new System.Security.SecureString(pass, 4);
      password.MakeReadOnly();
    }

    //gpg = new ExeGPG("d:/adammil/programs/gnupg/gpg.exe");
    gpg = new ExeGPG("c:/program files/gnu/gnupg/gpg2.exe");
    gpg.LineLogged += delegate(string line) { System.Diagnostics.Debugger.Log(0, "GPG", line+"\n"); };
    gpg.KeyPasswordNeeded += delegate(string keyId, string userIdHint) { return password.Copy(); };
    gpg.PlainPasswordNeeded += delegate() { return password.Copy(); };
    gpg.RetrieveKeySignatureFingerprints = true;

    keyring = new Keyring(Path.GetTempFileName(), Path.GetTempFileName(), Path.GetTempFileName());
    keyring.Create(gpg, true);
  }

  [Test]
  public void T01_ImportTestKeys()
  {
    // import some keys for testing
    MemoryStream secretKeys = new MemoryStream(Encoding.ASCII.GetBytes(@"
-----BEGIN PGP PRIVATE KEY BLOCK-----

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
H7EsOJ/JYySUpqz5AsaDd4LWqA==
=UXCB
-----END PGP PRIVATE KEY BLOCK-----
"));

    MemoryStream publicKeys = new MemoryStream(Encoding.ASCII.GetBytes(@"
-----BEGIN PGP PUBLIC KEY BLOCK-----

mQGiBEhoo0ERBACyL3SIclU+6GgoK3Q0yvxMNw9ZRl23pqqV4ep0vDUpPK80RFdX
9FP3jFHYgmTI+DtzbtDPjIPBwsGIbarFYAo+Uwd+EbO1KbaU5EprpZ3Q3Fv3ZV0R
TQa+qWTT2zfyzZp2j3OpaqCed5MWzayZxN4BfWxx1gw8RWlSE14bcq+38wCgnMY/
FMrMdVYsIrcNV1K+soYWrHsD/1ZBgCSioHd4L51d61w6pO+mQozL9RG44SwabEx3
SIY/OzOF0E/Oqii9nD69meOc7rJq105mG8PIprWN6/CLUqYq3jpMmRaVj3J5Yiy4
nNqiKLPXYHoO60/ERWyOFfJwoU82tuEnN8mbiwGYnRYaeNPA8rYIZIpqvgj/ZeaZ
i1HzA/4p+vqHV545s9v0GfDr8AYcIxT8VCVf7bYGe+9go/dWU6bxOWWSDO1Jrp+A
cClSf6wPqgYZgQ1Za67+JkR9UtEnFuh13dfyDgZFbLb96OLLIftyrjRrUdZcoWx+
I2lKYpvFGu33LmCkQoUBQqZ4Xk9VhIc9oFfZesLKUjAQqKlSBrQTRW5jcnlwdGVy
IDxlQHguY29tPohgBBMRAgAgBQJIaKNBAhsjBgsJCAcDAgQVAggDBBYCAwECHgEC
F4AACgkQSxt4SHKMOHxqYgCfVuzqWvX6EEw+X2tflsSRBzNoh9cAoJYSKrMr6M/1
NdlCblnwpW8ShwQM0dPR088BEAABAQAAAAAAAAAAAAAAAP/Y/+AAEEpGSUYAAQIB
AEgASAAA/+0BNFBob3Rvc2hvcCAzLjAAOEJJTQPtAAAAAAAQAEgAAAABAAEASAAA
AAEAAThCSU0D8wAAAAAACAAAAAAAAAAAOEJJTScQAAAAAAAKAAEAAAAAAAAAAjhC
SU0D9QAAAAAASAAvZmYAAQBsZmYABgAAAAAAAQAvZmYAAQChmZoABgAAAAAAAQAy
AAAAAQBaAAAABgAAAAAAAQA1AAAAAQAtAAAABgAAAAAAAThCSU0D+AAAAAAAcAAA
/////////////////////////////wPoAAAAAP//////////////////////////
//8D6AAAAAD/////////////////////////////A+gAAAAA////////////////
/////////////wPoAAA4QklNBAYAAAAAAAIAAv/uAA5BZG9iZQBkgAAAAAH/2wCE
AAwICAgJCAwJCQwRCwoLERUPDAwPFRgTExUTExgRDAwMDAwMEQwMDAwMDAwMDAwM
DAwMDAwMDAwMDAwMDAwMDAwBDQsLDQ4NEA4OEBQODg4UFA4ODg4UEQwMDAwMEREM
DAwMDAwRDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDP/AABEIALQAlgMBIgAC
EQEDEQH/xAE/AAABBQEBAQEBAQAAAAAAAAADAAECBAUGBwgJCgsBAAEFAQEBAQEB
AAAAAAAAAAEAAgMEBQYHCAkKCxAAAQQBAwIEAgUHBggFAwwzAQACEQMEIRIxBUFR
YRMicYEyBhSRobFCIyQVUsFiMzRygtFDByWSU/Dh8WNzNRaisoMmRJNUZEXCo3Q2
F9JV4mXys4TD03Xj80YnlKSFtJXE1OT0pbXF1eX1VmZ2hpamtsbW5vY3R1dnd4eX
p7fH1+f3EQACAgECBAQDBAUGBwcGBTUBAAIRAyExEgRBUWFxIhMFMoGRFKGxQiPB
UtHwMyRi4XKCkkNTFWNzNPElBhaisoMHJjXC0kSTVKMXZEVVNnRl4vKzhMPTdePz
RpSkhbSVxNTk9KW1xdXl9VZmdoaWprbG1ub2JzdHV2d3h5ent8f/3QAEAAr/2gAM
AwEAAhEDEQA/APVEkkkFKSSSSUpJJJJSz3srYXvIaxoJc4mAANSSSvOfrR/jfw8G
6zD6HS3NsZLXZTyRSHDT9G1vuvb/ACt9bEL/ABwfWmzGoq+r+JZtfkt9TNI59KYq
p/665u968jRAU9Zkf40vrpcHD7Y2oO49OqsFv9R21zkXG/xr/W6l+622nIbpLbKg
NO8Gn0/pLjkTHaHXMaRIJCNKfYPq9/jW6Z1F7aM9v2DIcYBc7dUT/wAd7dn/AFxd
1RmV2gEGZ4heBdYwOmiis0uDbtvujxW7/i8+t19GQOj5lhex5P2WxxJII/wEn8x3
+DQU+0gg8J1SwcsWsGuquoKUkkkkpSSSSSlJJJJKf//Q9USSSQUpJJJJSkkkklPz
t9fM52f9bup3FxeGXmlkiIbV+h2x/YWCWuAkgx4rpj0O7rn156jg1B0HLyHWFup2
ix/C3vrN/i6f0zpdnUOmB1tFDC7Kpe4PED6djIO5r6/pIqfOk7S4OBHPZExse3Ky
asakbrbnhjB4lx2tXqXTv8W/TOnYtdvWHUMdZA3ZNgbLj+azcdiNqfK7fWkOt3e7
gu7/AASptsotZdU4tsrcHscOQQZC7H6+/VR31f2PxmO/ZuQRtBO5rLIn9FZ+7Z+6
uLSCn3v6sdT+2YePkjQX1tfHgXCV1dbpbK82/wAXuRv6HhwSdgcwz4te7T/NXouM
ZYE0qTJJJJKUkkkkpSSSSSn/0fVEkkkFKSSSSUpJJJJT45hMyMH6xfXduK6c1uNk
vpe0e4B1gts9P87eyt6rfUDr5rZmdOc0vquwb22UtGllntbjO2/6az1bKnuR339R
6d/jT6jlY9chtjzcx+gdVY1v/VyxzF0XS+mYePvtpx66Ta82FrBDQSZ9o/k/mqlz
3xCHLRquPIdo9vGTPhwHJrtHu8P0n6t9a6Zn4mbd09zvs1zLHbbGlxAI3D05Vv8A
xn9XyrPrdW9rt+NjU1OxGESyHt32O2O9vus312f1F6IzEpvAFzA8DUT4oHVPq/0j
qjQ3MxmW7fovI9w/qvHuWfi+PEUc2McJNXj3/wAWXzMkuVj+jI/V5m7Ks6l/iiyr
8tu1teT+pgzo0Ws2MrP7jN91Vf8AIXmC9U/xgNvu+rDcXFyKWYPT3t3Y1TAyQPZW
z2HZ+j3furytbPLcxj5iHHjJIutRwkFrzhKBqT6p/i4P+RKP+Ms/6pem4n82PgvJ
/wDFnaD0x7N0mu90jwDmsIXquC6agpSsbSSSSClJJJJKUkkkkp//0vVEkkkFKSSS
SUpJJVuo9Qxem4N+flv9PHx2F9jvIdh/Kd+akp8k+smVlU/4zc3HxmCx2UaWbT3i
qtw/qrpcHqeK8enaHUWt0fW5pMEfShzQ5cb9V39T+tf1/d1UMj3OvvOu2usD0q65
1/kVsXd9S6RvyDZU80ZTNN8aO/rtWf8AEfh45gccf5weNcTYwZ+D0k+k/gzf1nEr
btrFlrvBrCP+k/YqV3UMnK9tn6GsmBU0y539dzf+oYofszqr3RZdW1vdwkk/Ja/S
+iwRtBJP0rXDX+z+6s7B8HyGQ448AH6UjxS/wIs8uYxxGh4y8j9dMTOH1dudXU0V
NLHWyfcGT9Nrf621cN0fo9XVabmV2+nmVEOa130HMPw930l9A5vQMDNwLcG5ssuY
a3OPPuEL59z8XqP1a63kYhca8nFeWb40c0/Qfr+ZbX7lucvghghwQursk7yk08mS
WSXFJ2fqRmW9K69Z03Klnr+yDx6jdaz/AG2r2jpd4cwCV832ZN9l5yXvJuLt2/vP
iF7P9S/rAzqfT6ryf0rfZe3wePpf5/01MVj3iSHTYHtBREFKSSSSUpJJJJT/AP/T
9USSSQUpJJJJSxIaCSYA1JK8T/xl/Xd3W84dL6c8/s7Dfq4QRda2W+q2P8Ez6NX/
AG4uj/xsfXO7CZ/ze6e/ZdkV7s6wctrfo3HafzXWt/nP+C/rrzb6sYwyevYTHNDm
NsD3A8EMmz/viKn2r/F39WG/V/oLDc2M/Ni/KPcSP0dH/Wmf+Cb1uZ/Tm5J9QO2v
Aj4pdPzm5FLfa4GNdFZtscxu4NkDlJTQx+jNa4Oudvj80BaLGNYIaAAo1PscPcyP
Ap3vLG7tpMdgkpmvK/8AHV0VgbhdcrbDyTi3kd9DbQ4/da1ei2dVYyR6bgR4hc79
csa76xfV3L6fQzfle23HZxufW4O2M/lvr9RjUlPhC3Pql9YT0PqIssk4tw23gakR
9Cxo/eYsb0bt7q9jt7J3sgyNv0tzf5Kgip+juj9Rqvprexwcx7Q5jhwQdWlbLXBw
leE/UP623dOyq+m5bycS522lx/wbyfaP+KevZ8DMFrRPPdNKnQSSGqSSlJJJJKf/
1PVEkkkFKUXvYxjnvO1jQXOJ4AGpUly3+MnrQ6R9VMotdtvzR9lpjmbAfVd/ZpbY
kFPinV80dW6x1Dqlzia7bX2DXUhzttFTS7/g/wDoMXc/4pPqmMoW9eymxU1xqxQe
5H87Z/Z/m/8AtxcJi4GRnZeH0fGE3ZD27gP37I+l/wAVUvovpXTsbpfTsfp2KNtG
LWK2ecfSe7+U93vcipssY1jdrQAB2ScA4QU6iXQ8M8QSgpkkkkkpZzGPEOaHDzVH
J6Yw/pMf2WDUD4K+klanxj/Gl0I4mbT9YMRpqGW41ZgbptyAJ9TT/uTX/wCCV2Li
7K3X1b3M23gbpAgPb+dp/pGr3n/GD01vUPqh1GvaDZTX9oZ47qT6v/ntr2LwWzqN
jm1tY0NFUFvyRU1ASDIMEaghe2fUrrZ6h0zHyHH9IRstH8tntf8A53014zlsYy93
piK3gPYPJw3rsf8AFn1I15WRgOOjwLqx5t9ln5WJUp9tpfuYCiKj063fUNVeQUpJ
JJJT/9X1RJJJBSivDf8AGN9ZG/WD6zV4VDi7BwHegyOH2bv1i0f5vpsXqn166jZ0
36pdSyqnFloq9Nj28h1pbQHf+CLwDpfoDPpNz9jA4Hd8+6IU9t/igwPt/wBZsrql
on7HUXN8BZcdjf8ANr9VezLzX/EvjCqnrDu4uZV/mh//AJJelJFSkKf1qPBv8UVB
H9KcfBoCCkySSSSlJJJJKa/UKRf0/JoOotpsYR/Wa5q+YF9SXECl5OgDTP3L5eZX
6lwYOHO58v8AzlFSTMJNjGn8yutv/RC0fqjk/Z/rDhuJAD3Gsk/ywWD/AKSyr7PV
ufZ2cdPh+b/0Vb6F/wAtYH/hiv8A6oIqff8Ao1ksAW2OFz3Rey6AcBNUukkkkp//
1vVEpVfPza8HEsyXiQwaNHJJ0a3+05c8/r/VrBurbVWDw2C7/pbgqvMc5h5cgZJU
ZagAWy48M8gJiNm/9cMXDzvq9l4OW4hmS0MZt59QEPpLf6r2blwI/wAXXQXYXpD1
W5EaZG+Tu8fT+htXS3WZ2a9r8t4IZ9FjRDR5owECFic/8VyTyD7vOUIR7eniP9Zu
YeVjGPrAlI/g8n9SetD6ndWv6J11vpV5jmmnOH0HbZYx9n8l0/T/AO3F6u1zXNDm
kOa4SCNQQe4Xnn1n6BV1zpzqYAyagX47/B37n9R65b6nf4xeo/Vy79l9WD8jp7He
mWnWygg7XenP02f8F/mLX+Hc6OaxWdMsNMkfymP7zUz4fblp8p2fbUJv9Jf/AFQo
dP6hhdSxK8zBubkY9olljDI+H8lyN6bQ/wBQfSIgq6wskkkklKSJgEnQDukqudmV
47NrgXOeDAHh4pHYqDndS6s/I6Zl14NZN9lVjKHuIDdzmlrHO13bV89WPfQ59DCG
hhLXEckjR3u/dXtTrS3DIaYJaRp5rxvqnTsrCy7K7WGC4lj4MOBPMrM+F89l5iWU
ZjH01wADh/vNnmMAgAYg+LXrvskNIbYOA1wldL9Wulsv+sFORiVu+z4o33mZax5a
Qxu7+U9YGD0zOzbW141LnEmN0ENH9Z69Z+rPSaujdLbjM91r5dkWfvOI/wCpb9Fq
n5/4hDloiqnOR0hf6P6Uisw4JZL6Dv4vTdFAgLdHC5jot59rT8101ZloKtg2Ae4Y
SySSSRU//9f0D6yMLukWmSNjmO08nN+l/JWFQdzAuh665rOkZRcYlkD4kgBc5ifz
QJXO/HR+tgf6v7XQ5I/qz5pkkklitpXeV53/AIxfq/6OQOs4zf0VxDcgDs/hr/7a
9EQMvEozcW3EyGh9VzS1wPmrXJc0eWzRyDWPyzj+9BizYxkgR9j5d9SvrbnfVzqt
Tq3l2De9rcrH/Nc0nb6jR+bbX+a5fQg4leXfVL/Ff0ezJdlZ+U/JdiW64QaGN0O6
k2vlzrK3t/d9NeorsIzjOInE3GQsFyyCCQdwpJJJFClkddIri5zg1jWGXEwBC0sv
MxcLHsysu1tGPUN1lryA0D4rx364/wCMF/1iz6um9MBq6YLGh7yIfdBn3D8yn/g/
89A6AnwSNwHrMX9NQAq9vT2uOon4qz0zWlvmFd2g8hcWchhOXDpq7PRz8TArY4Ha
NFoBoGgSgDgJ1HOZkbJUjpcce8OAhrjqfArpMPLZYwarniB34SoyjTeGsJg9vBb3
wr4lKfDy+QEy2hk8I/ozaPM8uBeSJ/vR/g9buESkqLchxxHP7gD8qS22m//Q7P63
ZBFeNiTDbXF7z4hkbW/5z1nUuYGAA6Lo+q9JxepVNbcCHMkssaYcJ/76ubyvq11C
gk4uQLGjhtgg/wCc2f8AqVifEvh+fPlOSPqjQoW3eWz44wEZaG0oITyse79s4gm7
GeQPzme8f9DchVfWBm7a/QjkHQ/isifI54fNAj6NuOSEtiC7qY8KlV1XGeBJhSv6
jQxhIcCoPandUVzl9X6z1DouQ3qeAQbWCLanfQtZ3rfH/QervT/8cv1cvrH26jIw
7fzgGi1k/wAl7C1//gS5fr3UmPY+T7QuYxfqj9YM2tuRj4hNVvuYS5rZB4dtc7cu
p+FCY5fhl02c7mxHjBHV9bs/xt/Uxn0br7PJtLv+/wCxY3U/8dmE1pb0rp9lr+1m
S4MaPP06vUc7/txi40f4tvrOeGU/9uf+Yp//ABtPrR+5T/25/wCYrQazmdf+tXXP
rDd6nUsl1jAZrx2+2pn9Sof9W/8ASKt0Zm/qNPkZW1Z/i3+tNbC4U12H91lgn/p7
WoNH1c670fKbkZ+I+mkQDbIc0Fx0G5hcm5f5udfuldj+Yeb6V0v+YaPAK+sDpWcA
wAlahz6g2SVxebHITOnV1wdG2TCFZk1VN3PcAsy/qbrX+lQC+w9h28yruD0t9zw+
33v8+B/VCt8n8Ly59T6IdZH/ALliy8xDHpvLst62RkaVNLWn84/3K/gdLduDnTJ5
JWph9LYwCQtGuhrBAC6DleRwcsPRG5fvy1k0MueeTfQfuhCMaMZ1fiElbhJWmJ//
0fVFEsaeykkgpC/Grd2VHM6Fg5YIvoZZP7zQT/nLUSSOu6reJ6h9Q2av6de7Gd2Y
6Xs/H9I3/OXPZH1V+tDLBXtrsYf8ILIH+a4b/wDor1YtBUHUsd2UMuWwyNmAHkyj
PkGgL550f6nOot9bNLci780R7G/1d30n/wApddhdKa1okLUbj1g8IgaBwpYxERUR
QYzIk2Wu3CrHZS+y1+AR0kUIPstfgq3UOjYfUMO3DyWb6rRDgND4tc0/vNd7loJJ
KfM8v6lde6dYRg2MzcefbuPp2Afy936N39lyFX0H6w2nbaxmO2dXF28/2W1/+SXp
7mB3IQzi1nsqx5PAZcRizDmMgFW8n0n6vjGaBq951fY7lxXS4mGyto0VltDG8BEA
hWAAAABQHRiJJNlYABOkkihSSSSSn//S9USXyskgp+qUl8rJJKfqlJfKySSn6pSX
yskkp+qUl8rJJKfqlJfKySSn6pSXyskkp+qUl8rJJKfqlJfKySSn6pSXyskkp//Z
iGAEExECACAFAkhqv/4CGyMGCwkIBwMCBBUCCAMEFgIDAQIeAQIXgAAKCRBLG3hI
cow4fB2NAJ4zBtvFamu76V7dBM5bXIB0E7e6AgCfZNKekRN07whMrP0UMTIXbFcc
3pK5AQ0ESGijQRAEAKNCXeQJchMR4EeyCO71+Gp70ctBilWPHDUDhY0LDXxxd7GV
H9emI/xrVbFUqofn4xXnpDeGi3Z2T1fEYEtP7DY+Jrx4iixBrMwXx1asph9tMeSw
3E970FSUbaWwPnHWF4FcA9FArLl7NL633neyOeDnv7LFs5b81e28jOfSwCKXAAMH
A/9F77TyW1Nu24QOyqa56sYeEQSfSsiBo9S8wQFFtakidhAdBfRhEd69Q15rApJ4
vvs3yVAcO6HqhrP02l7oT4pbONG/k1mzB877xQao0j+okJfbhcSa8kZjpXuwHadr
c+9SpfXZM0A+f4q3Zf0nFbWxPHUGIooFWUrdrsREukwBuIhJBBgRAgAJBQJIaKNB
AhsMAAoJEEsbeEhyjDh8lS0An3nIBdI9YfOF7arIxqUKiWa+Qf04AJ9G+zQIZzYq
EXbkhkXpYsw9b7QJo5iNBEhoo0UBBADSOfptHuzt0YI9O+M49HefEpunH+w8tNjC
ylxZ9SHbsoC50RX76gl+fDDjJDFFcJHkuoqyQ3+BTzzsx7A9dqYYv5aJX61rKSIm
qmuhOIw4Ux7Jy+HcYm3Ewd8ijFClwgEuXyFBFocaOlAzAtLevNlnJFZUVb2UG4Lg
5Jt3WIVoZwARAQABtBBTaWduZXIgPHNAeC5jb20+iLYEEwECACAFAkhoo0UCGy8G
CwkIBwMCBBUCCAMEFgIDAQIeAQIXgAAKCRBSLD6/itLexItWA/98OZydONIUpjKu
w9cVnzO6kNgEJ/H+Rn+hx2xlsGiEWZWnmJZJe5xhbZY0rjqTSrjEMiRhXexeYD+1
MfSgkNKnfoVEO5DdRi5Uu6hlouhlGB3XGBrB/GD728Nee/kRVLePfyVMzTvRz4vf
2yKyDYv+gL3v8EiVcxoNJ7Bwmp4qGpiNBEhoo0YBBADW4RXC8n3noEQJATYn381m
8Y1md4PhUc0jlxuHgecwWC/eZcARfFOjRKeLC0U39q+TS45w7pGw0V58w9dQmt8Z
PniHNZqI3L1MZwJ9yBhbI5uzkefxu3Xi1lkEItuM9uaPAWh6jua8boGnLfmRYcs4
2+fwhLec1w3qwSgf4OhFJwARAQABtBJSZWNlaXZlciA8ckB4LmNvbT6ItgQTAQIA
IAUCSGijRgIbLwYLCQgHAwIEFQIIAwQWAgMBAh4BAheAAAoJEBOCnD/MlopQTykD
/16f/C7vJXt4PezxVgjG9Oukc82Da6OIv2Jz2UgSeENb9MWhZzAo3Rzx/B5aHndx
3XY16Q+fKkpQt6f7M6LvTL7LJAB3jcg/SxQ1W5uXkXnqjPMuMhmA7LSuV6O5p779
3PJbvuztqEZRROuka9wfsSw4n8ljJJSmrPkCxoN3gtao
=gHB+
-----END PGP PUBLIC KEY BLOCK-----
"));

    // import the secret keys
    ImportedKey[] results = gpg.ImportKeys(secretKeys, keyring, ImportOptions.ImportLocalSignatures);
    Assert.AreEqual(3, results.Length);
    foreach(ImportedKey key in results) Assert.IsTrue(key.Successful);

    // import the public keys, including the photograph(s)
    results = gpg.ImportKeys(publicKeys, keyring, ImportOptions.ImportLocalSignatures);
    Assert.AreEqual(3, results.Length);
    foreach(ImportedKey key in results) Assert.IsTrue(key.Successful);

    // read the keys (which also indicates that everything was imported)
    keys = gpg.GetPublicKeys(keyring);

    // set the trust level of the encryption key to Ultimate
    gpg.SetTrustLevel(keys[Encrypter], TrustLevel.Ultimate);
  }

  [Test]
  public void T02_Hashing()
  {
    byte[] hash = gpg.Hash(new MemoryStream(Encoding.ASCII.GetBytes("The quick brown fox jumps over the lazy dog")),
                           HashAlgorithm.SHA1);
    CollectionAssert.AreEqual(new byte[] { 
      0x2f, 0xd4, 0xe1, 0xc6, 0x7a, 0x2d, 0x28, 0xfc, 0xed, 0x84,
      0x9e, 0xe1, 0xbb, 0x76, 0xe7, 0x39, 0x1b, 0x93, 0xeb, 0x12 }, hash);
  }

  [Test]
  public void T03_RandomData()
  {
    byte[] random = new byte[100];
    gpg.GetRandomData(Randomness.Weak, random, 0, random.Length);
  }

  [Test]
  public void T04_Signing()
  {
    EnsureImported();
    MemoryStream plaintext = new MemoryStream(Encoding.UTF8.GetBytes("Hello, world!")), signature = new MemoryStream();

    // we'll use only the test keyring
    VerificationOptions options = new VerificationOptions();
    options.AdditionalKeyrings.Add(keyring);
    options.IgnoreDefaultKeyring = true;

    // test ASCII embedded signatures
    gpg.Sign(plaintext, signature, new SigningOptions(keys[Signer], keys[Encrypter]),
             new OutputOptions(OutputFormat.ASCII, "Woot"));

    // verify the signature
    signature.Position = 0;
    CheckSignatures(keys, gpg.Verify(signature, options));

    // check that it contains the comment
    signature.Position = 0;
    string asciiSig = new StreamReader(signature).ReadToEnd();
    Assert.IsTrue(asciiSig.Contains("Comment: Woot"));

    // test binary detached signatures
    plaintext.Position = 0;
    signature = new MemoryStream();
    gpg.Sign(plaintext, signature, new SigningOptions(true, keys[Signer], keys[Encrypter]), null);

    // verify the signature
    plaintext.Position = signature.Position = 0;
    CheckSignatures(keys, gpg.Verify(signature, plaintext, options));
  }

  [Test]
  public void T05_Encryption()
  {
    EnsureImported();

    const string PlainTextString = "Hello, world!";
    MemoryStream plaintext  = new MemoryStream(Encoding.UTF8.GetBytes(PlainTextString));
    MemoryStream ciphertext = new MemoryStream(), decrypted = new MemoryStream();

    // use only the test keyring
    DecryptionOptions decryptOptions = new DecryptionOptions();
    decryptOptions.AdditionalKeyrings.Add(keyring);
    decryptOptions.IgnoreDefaultKeyring = true;

    // test ASCII key-based encryption
    EncryptionOptions encryptOptions = new EncryptionOptions(keys[Encrypter], keys[Receiver]);
    encryptOptions.AlwaysTrustRecipients = true;
    gpg.Encrypt(plaintext, ciphertext, encryptOptions, new OutputOptions(OutputFormat.ASCII, "Woot"));

    // verify that it decrypts properly
    ciphertext.Position = 0;
    gpg.Decrypt(ciphertext, decrypted, decryptOptions);
    decrypted.Position = 0;
    Assert.AreEqual(PlainTextString, new StreamReader(decrypted).ReadToEnd());

    // verify that it contains the comment
    ciphertext.Position = 0;
    string asciiCiphertext = new StreamReader(ciphertext).ReadToEnd();
    Assert.IsTrue(asciiCiphertext.Contains("Comment: Woot"));

    // test binary password-based encryption
    ciphertext = new MemoryStream();
    plaintext.Position = 0;
    gpg.Encrypt(plaintext, ciphertext, new EncryptionOptions(password));

    // verify that it decrypts properly
    decrypted = new MemoryStream();
    ciphertext.Position = 0;
    gpg.Decrypt(ciphertext, decrypted, new DecryptionOptions(password));
    decrypted.Position = 0;
    Assert.AreEqual(PlainTextString, new StreamReader(decrypted).ReadToEnd());

    // test signed, encrypted data
    encryptOptions = new EncryptionOptions(keys[Encrypter]);
    encryptOptions.AlwaysTrustRecipients = true;
    ciphertext = new MemoryStream();
    plaintext.Position = 0;
    gpg.SignAndEncrypt(plaintext, ciphertext, new SigningOptions(keys[Signer], keys[Encrypter]), encryptOptions);

    // verify the signatures
    decrypted = new MemoryStream();
    ciphertext.Position = 0;
    CheckSignatures(keys, gpg.Decrypt(ciphertext, decrypted, decryptOptions));

    // verify that it decrypted properly
    decrypted.Position = 0;
    Assert.AreEqual(PlainTextString, new StreamReader(decrypted).ReadToEnd());
  }

  [Test]
  public void T06_Export()
  {
    EnsureImported();
    
    // test ascii armored public keys
    MemoryStream output = new MemoryStream();
    gpg.ExportPublicKey(keys[Encrypter], output, ExportOptions.Default,
                        new OutputOptions(OutputFormat.ASCII, "Woot"));

    // verify that it contains the comment
    output.Position = 0;
    Assert.IsTrue(new StreamReader(output).ReadToEnd().Contains("Comment: Woot"));

    // verify that it imports
    output.Position = 0;
    ImportedKey[] imported = gpg.ImportKeys(output, keyring);
    Assert.AreEqual(1, imported.Length);
    Assert.IsTrue(imported[0].Successful);
    Assert.AreEqual(imported[0].Fingerprint, keys[Encrypter].Fingerprint);

    // test binary secret keys
    output = new MemoryStream();
    gpg.ExportSecretKey(keys[Encrypter], output);

    // verify that it doesn't import (because the secret key already exists)
    output.Position = 0;
    TestHelpers.TestException<ImportFailedException>(delegate { gpg.ImportKeys(output, keyring); });

    // then delete the existing key and verify that it does import
    gpg.DeleteKey(keys[Encrypter], KeyDeletion.PublicAndSecret);
    Assert.AreEqual(2, gpg.GetPublicKeys(keyring).Length);
    output.Position = 0;
    imported = gpg.ImportKeys(output, keyring);
    Assert.AreEqual(1, imported.Length);
    Assert.IsTrue(imported[0].Successful);
    Assert.AreEqual(imported[0].Fingerprint, keys[Encrypter].Fingerprint);

    // then delete all of the keys and reimport them to restore things to the way they were
    keys = gpg.GetPublicKeys(keyring);
    Assert.AreEqual(3, keys.Length);
    gpg.DeleteKeys(keys, KeyDeletion.PublicAndSecret);
    Assert.AreEqual(0, gpg.GetPublicKeys(keyring).Length);
    T01_ImportTestKeys();
    Assert.AreEqual(3, gpg.GetPublicKeys(keyring).Length);
  }

  [Test]
  public void T07_KeyCreation()
  {
    EnsureImported();
    NewKeyOptions options = new NewKeyOptions();
    options.KeyType    = PrimaryKeyType.RSA;
    options.RealName   = "New Guy";
    options.Email      = "email@foo.com";
    options.Comment    = "Weird";
    options.Expiration = new DateTime(2090, 8, 1, 0, 0, 0, DateTimeKind.Utc);
    options.Password   = password;
    options.Keyring    = keyring;
    options.SubkeyType = SubkeyType.None;

    // create and delete the key
    Assert.AreEqual(3, gpg.GetPublicKeys(keyring).Length);
    PrimaryKey key = gpg.CreateKey(options);
    Assert.IsNotNull(key);
    Assert.AreEqual(PrimaryKeyType.RSA, key.KeyType);
    Assert.AreEqual(1, key.UserIds.Count);
    Assert.AreEqual(0, key.Subkeys.Count);
    Assert.AreEqual("New Guy (Weird) <email@foo.com>", key.UserIds[0].Name);
    Assert.IsTrue(key.ExpirationTime.HasValue);
    Assert.AreEqual(options.Expiration, key.ExpirationTime.Value.Date);
    Assert.AreEqual(4, gpg.GetPublicKeys(keyring).Length);
    gpg.DeleteKey(key, KeyDeletion.PublicAndSecret);
  }

  [Test]
  public void T08_Attributes()
  {
    EnsureImported();

    PrimaryKey[] keys = gpg.GetPublicKeys(keyring, ListOptions.RetrieveAttributes);

    Assert.AreEqual(1, keys[Encrypter].Attributes.Count);
    Assert.IsTrue(keys[Encrypter].Attributes[0] is UserImage);

    UserImage imageAttr = (UserImage)(keys[Encrypter].Attributes[0]);

    CollectionAssert.AreEqual(new byte[] { 0xed, 0xe6, 0x05, 0xf4, 0xff, 0x4b, 0x84, 0xca, 0xf7, 0x73,
                                           0xa2, 0x2b, 0x37, 0x19, 0xf1, 0x94, 0x71, 0x90, 0x7e, 0xd6 }, 
                              gpg.Hash(imageAttr.GetSubpacketStream(), HashAlgorithm.SHA1));

    using(System.Drawing.Bitmap bitmap = imageAttr.GetBitmap())
    {
      Assert.AreEqual(150, bitmap.Width);
      Assert.AreEqual(180, bitmap.Height);
    }
  }

  [Test]
  public void T09_EditUidAndSig()
  {
    EnsureImported();

    UserPreferences preferences = new UserPreferences();
    preferences.Keyserver = new Uri("hkp://keys.foo.com");
    preferences.PreferredCiphers.Add(OpenPGPCipher.AES256);
    preferences.PreferredCiphers.Add(OpenPGPCipher.AES);
    preferences.PreferredCiphers.Add(OpenPGPCipher.Twofish);
    preferences.PreferredCiphers.Add(OpenPGPCipher.CAST5);
    preferences.PreferredCompressions.Add(OpenPGPCompression.Bzip2);
    preferences.PreferredCompressions.Add(OpenPGPCompression.Zlib);
    preferences.PreferredCompressions.Add(OpenPGPCompression.Zip);
    preferences.PreferredHashes.Add(OpenPGPHashAlgorithm.SHA1);
    preferences.PreferredHashes.Add(OpenPGPHashAlgorithm.MD5);

    // test adding a user ID
    gpg.AddUserId(keys[Encrypter], "John", "john@gmail.com", "big man", preferences);

    keys[Encrypter] = gpg.RefreshKey(keys[Encrypter]);
    Assert.AreEqual(2, keys[Encrypter].UserIds.Count);
    UserId newId = keys[Encrypter].UserIds[1];
    Assert.IsFalse(newId.Primary);
    Assert.AreEqual("John (big man) <john@gmail.com>", newId.Name);

    // test GetPreferences(), and ensure that AddUserId() added the preferences properly
    UserPreferences newPrefs = gpg.GetPreferences(newId);
    // TODO: Assert.AreEqual(preferences.Keyserver, newPrefs.Keyserver);
    Assert.AreEqual(preferences.Primary, newPrefs.Primary);
    CollectionAssert.AreEqual(preferences.PreferredCiphers, newPrefs.PreferredCiphers);
    CollectionAssert.AreEqual(preferences.PreferredCompressions, newPrefs.PreferredCompressions);
    CollectionAssert.AreEqual(preferences.PreferredHashes, newPrefs.PreferredHashes);

    // test SetPreferences()
    preferences.PreferredCiphers.Remove(OpenPGPCipher.Twofish);
    preferences.PreferredCompressions.Remove(OpenPGPCompression.Bzip2);
    gpg.SetPreferences(newId, preferences);
    newPrefs = gpg.GetPreferences(newId);
    Assert.AreEqual(preferences.Primary, newPrefs.Primary);
    CollectionAssert.AreEqual(preferences.PreferredCiphers, newPrefs.PreferredCiphers);
    CollectionAssert.AreEqual(preferences.PreferredCompressions, newPrefs.PreferredCompressions);
    CollectionAssert.AreEqual(preferences.PreferredHashes, newPrefs.PreferredHashes);

    // test key signing (sign Receiver's key with Encrypter's key)
    keys = gpg.GetPublicKeys(keyring, ListOptions.RetrieveSignatures);
    Assert.AreEqual(keys[Receiver].UserIds[0].Signatures.Count, 1);
    gpg.SignKey(keys[Receiver], keys[Encrypter], null);
    keys[Receiver] = gpg.RefreshKey(keys[Receiver], ListOptions.RetrieveSignatures);
    Assert.AreEqual(2, keys[Receiver].UserIds[0].Signatures.Count);
    KeySignature keySig = keys[Receiver].UserIds[0].Signatures[1];
    Assert.IsFalse(keySig.Exportable);
    Assert.AreEqual(keys[Encrypter].KeyId, keySig.KeyId);
    Assert.AreEqual(keys[Encrypter].Fingerprint, keySig.KeyFingerprint);
    Assert.AreEqual(OpenPGPSignatureType.GenericCertification, keySig.Type);
    Assert.AreEqual(keys[Encrypter].PrimaryUserId.Name, keySig.SignerName);

    // test UID signing (sign Encrypter's new UID with Signer's key)
    KeySigningOptions ksOptions = new KeySigningOptions(true, true);
    ksOptions.TrustDepth  = 2;
    ksOptions.TrustDomain = "Mu.*";
    ksOptions.TrustLevel  = TrustLevel.Marginal;
    gpg.SignKey(keys[Encrypter].UserIds[1], keys[Signer], ksOptions);
    keys[Encrypter] = gpg.RefreshKey(keys[Encrypter], ListOptions.RetrieveSignatures);
    Assert.AreEqual(1, keys[Encrypter].UserIds[0].Signatures.Count);
    Assert.AreEqual(2, keys[Encrypter].UserIds[1].Signatures.Count);
    keySig = keys[Encrypter].UserIds[1].Signatures[1];
    Assert.IsTrue(keySig.Exportable);
    Assert.AreEqual(keys[Signer].KeyId, keySig.KeyId);
    Assert.AreEqual(keys[Signer].Fingerprint, keySig.KeyFingerprint);
    Assert.AreEqual(OpenPGPSignatureType.GenericCertification, keySig.Type);
    Assert.AreEqual(keys[Signer].PrimaryUserId.Name, keySig.SignerName);

    // test signature revocation
    gpg.RevokeSignatures(new UserRevocationReason(UserRevocationCode.Unspecified,
                                                   "i thought he was a nice guy...\n\nbut he's not\n"),
                         keys[Receiver].UserIds[0].Signatures[1]);
    keys[Receiver] = gpg.RefreshKey(keys[Receiver], ListOptions.RetrieveSignatures);
    Assert.AreEqual(3, keys[Receiver].UserIds[0].Signatures.Count);

    bool revoked = false;
    foreach(KeySignature sig in keys[Receiver].UserIds[0].Signatures)
    {
      if(sig.Revocation) revoked = true;
    }
    Assert.IsTrue(revoked);

    // test signature deletion
    gpg.DeleteSignatures(keys[Receiver].UserIds[0].Signatures[0], keys[Encrypter].UserIds[1].Signatures[1],
                         keys[Receiver].UserIds[0].Signatures[1]);
    keys = gpg.RefreshKeys(keys, ListOptions.RetrieveSignatures);
    Assert.AreEqual(1, keys[Receiver].UserIds[0].Signatures.Count);
    Assert.AreEqual(1, keys[Encrypter].UserIds[1].Signatures.Count);

    // test UID revocation
    gpg.RevokeAttributes(null, keys[Encrypter].UserIds[1]);
    keys[Encrypter] = gpg.RefreshKey(keys[Encrypter]);
    Assert.IsTrue(keys[Encrypter].UserIds[1].Revoked);

    // test adding a photo ID
    Random rnd = new Random();
    using(Bitmap bmp = new Bitmap(20, 30))
    {
      for(int y=0; y<bmp.Height; y++)
      {
        for(int x=0; x<bmp.Width; x++) bmp.SetPixel(x, y, Color.FromArgb(rnd.Next(256), rnd.Next(256), rnd.Next(256)));
      }
      gpg.AddPhoto(keys[Encrypter], bmp, preferences);
    }

    // verify that the photo was added correctly
    keys[Encrypter] = gpg.RefreshKey(keys[Encrypter], ListOptions.RetrieveAttributes);
    Assert.AreEqual(2, keys[Encrypter].Attributes.Count);
    Assert.IsTrue(keys[Encrypter].Attributes[1] is UserImage);
    UserImage userImage = (UserImage)keys[Encrypter].Attributes[1];
    using(Bitmap bmp = userImage.GetBitmap())
    {
      Assert.AreEqual(bmp.Width, 20);
      Assert.AreEqual(bmp.Height, 30);
    }

    // test attribute deletion. ensure that you can't delete all the user IDs
    TestHelpers.TestException<PGPException>(delegate { gpg.DeleteAttributes(keys[Encrypter].UserIds.ToArray()); });
    // delete the new UID and photo
    gpg.DeleteAttributes(keys[Encrypter].UserIds[1], keys[Encrypter].Attributes[1]);
    keys[Encrypter] = gpg.RefreshKey(keys[Encrypter], ListOptions.RetrieveAttributes);
    Assert.AreEqual(1, keys[Encrypter].UserIds.Count);
    Assert.AreEqual(1, keys[Encrypter].Attributes.Count);
    Assert.AreNotEqual("John (big man) <john@gmail.com>", keys[Encrypter].UserIds[0].Name);
    using(Bitmap bmp = ((UserImage)keys[Encrypter].Attributes[0]).GetBitmap()) Assert.AreNotEqual(bmp.Width, 20);

    // set the trust level the reciever key to Marginal
    Assert.AreNotEqual(TrustLevel.Marginal, keys[Receiver].OwnerTrust);
    gpg.SetTrustLevel(keys[Receiver], TrustLevel.Marginal);
    keys[Receiver] = gpg.RefreshKey(keys[Receiver]);
    Assert.AreEqual(TrustLevel.Marginal, keys[Receiver].OwnerTrust);

    // delete the keys and reimport them to put everything back how it was
    gpg.DeleteKeys(keys, KeyDeletion.PublicAndSecret);
    T01_ImportTestKeys();
  }

  [Test]
  public void T10_EditKeyAndSubkey()
  {
    EnsureImported();

    // add a new subkey to Encrypter's key
    DateTime expiration = new DateTime(2100, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    gpg.AddSubkey(keys[Encrypter], SubkeyType.RSAEncryptOnly, 1500, expiration);
    keys[Encrypter] = gpg.RefreshKey(keys[Encrypter]);
    Assert.AreEqual(2, keys[Encrypter].Subkeys.Count);
    // GPG generates RSA rather than RSAEncryptOnly as per RFC-4880 section 13.5, and uses key capabilities instead
    Assert.AreEqual(SubkeyType.RSA, keys[Encrypter].Subkeys[1].KeyType);
    Assert.IsFalse((keys[Encrypter].Subkeys[1].Capabilities & KeyCapability.Sign) != 0);
    Assert.IsTrue(keys[Encrypter].Subkeys[1].Length >= 1500); // GPG may round the key size up
    Assert.IsTrue(keys[Encrypter].Subkeys[1].ExpirationTime.HasValue);
    Assert.AreEqual(expiration.Date, keys[Encrypter].Subkeys[1].ExpirationTime.Value.Date);

    // change the expiration of the primary key and first subkey
    expiration = new DateTime(2101, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    gpg.ChangeExpiration(keys[Encrypter], expiration);
    gpg.ChangeExpiration(keys[Encrypter].Subkeys[0], expiration);
    keys[Encrypter] = gpg.RefreshKey(keys[Encrypter]);
    Assert.IsTrue(keys[Encrypter].ExpirationTime.HasValue);
    Assert.AreEqual(expiration.Date, keys[Encrypter].ExpirationTime.Value.Date);
    Assert.IsTrue(keys[Encrypter].Subkeys[0].ExpirationTime.HasValue);
    Assert.AreEqual(expiration.Date, keys[Encrypter].Subkeys[0].ExpirationTime.Value.Date);

    // test subkey revocation
    gpg.RevokeSubkeys(new KeyRevocationReason(KeyRevocationCode.KeyRetired, "byebye"), keys[Encrypter].Subkeys[1]);
    keys[Encrypter] = gpg.RefreshKey(keys[Encrypter]);
    Assert.IsTrue(keys[Encrypter].Subkeys[1].Revoked);

    // remove a password and put it back
    gpg.ChangePassword(keys[Encrypter], null);
    gpg.ChangePassword(keys[Encrypter], password);

    // clean, minimize, disable, and reenable the keys
    gpg.CleanKeys(keys);
    gpg.MinimizeKeys(keys);
    gpg.DisableKeys(keys);
    gpg.EnableKeys(keys);

    // delete all subkeys from Encrypter
    gpg.DeleteSubkeys(keys[Encrypter].Subkeys.ToArray());

    // allow Signer to revoke Encrypter's key
    gpg.AddDesignatedRevoker(keys[Encrypter], keys[Signer]);
    keys[Encrypter] = gpg.RefreshKey(keys[Encrypter]);
    Assert.AreEqual(1, keys[Encrypter].DesignatedRevokers.Count);
    Assert.AreEqual(keys[Signer].Fingerprint, keys[Encrypter].DesignatedRevokers[0]);

    // revoke Encrypter's key using the designated revoker
    gpg.RevokeKeys(keys[Signer], null, keys[Encrypter]);

    // revoke Signer's key directly
    gpg.RevokeKeys(null, keys[Signer]);
    keys = gpg.RefreshKeys(keys);
    Assert.IsTrue(keys[Encrypter].Revoked);
    Assert.IsTrue(keys[Signer].Revoked);

    // delete the keys and reimport them to put everything back how it was
    gpg.DeleteKeys(keys, KeyDeletion.PublicAndSecret);
    T01_ImportTestKeys();
  }

  [Test]
  public void T11_KeyServer()
  {
    KeyDownloadOptions downloadOptions = new KeyDownloadOptions(new Uri("hkp://pgp.mit.edu"));

    // try searching for keys
    List<PrimaryKey> keysFound = new List<PrimaryKey>();
    gpg.FindPublicKeysOnServer(downloadOptions.KeyServer,
                         delegate(PrimaryKey[] keys) { keysFound.AddRange(keys); return true; }, "adam@adammil.net");
    Assert.AreEqual(1, keysFound.Count);
    Assert.IsTrue(keysFound[0].PrimaryUserId.Name.StartsWith("Adam M"));

    // import the key found
    ImportedKey[] result = gpg.ImportKeysFromServer(downloadOptions, keyring, keysFound[0].EffectiveId);
    Assert.AreEqual(1, result.Length);
    Assert.IsTrue(result[0].Successful);
    Assert.AreEqual("3B0592E6818F19CF853BB9E172DFF658727BA638", result[0].Fingerprint);

    // make sure it was really imported
    PrimaryKey adamsKey = gpg.FindPublicKey("727BA638", keyring);
    Assert.IsNotNull(adamsKey);
    Assert.IsTrue(adamsKey.PrimaryUserId.Name.StartsWith("Adam M"));
    // TODO: Assert.IsNotNull(gpg.GetPreferences(adamsKey.PrimaryUserId).Keyserver);

    // try searching by name
    adamsKey = gpg.FindPublicKey("adam", keyring);
    Assert.IsNotNull(adamsKey);
    Assert.IsTrue(adamsKey.PrimaryUserId.Name.StartsWith("Adam M"));

    // try searching by email
    adamsKey = gpg.FindPublicKey("adam@adammil.net", keyring);
    Assert.IsNotNull(adamsKey);
    Assert.IsTrue(adamsKey.PrimaryUserId.Name.StartsWith("Adam M"));

    // refresh the key
    result = gpg.RefreshKeysFromServer(downloadOptions, adamsKey);
    Assert.AreEqual(1, result.Length);
    Assert.IsTrue(result[0].Successful);

    // refresh the entire keyring
    result = gpg.RefreshKeysFromServer(downloadOptions, keyring);
    Assert.AreEqual(1, result.Length);
    Assert.IsTrue(result[0].Successful);

    // upload the key back to the key server
    gpg.UploadKeys(new KeyUploadOptions(downloadOptions.KeyServer), adamsKey);

    // delete the key from the keyring
    gpg.DeleteKey(adamsKey, KeyDeletion.PublicAndSecret);
  }

  void CheckSignatures(PrimaryKey[] keys, Signature[] sigs)
  {
    Assert.AreEqual(sigs.Length, 2);
    Assert.IsTrue(sigs[0].IsValid);
    Assert.IsTrue(sigs[1].IsValid);
    Assert.AreEqual(keys[Signer].Fingerprint, sigs[0].KeyFingerprint);
    Assert.AreEqual(keys[Encrypter].Fingerprint, sigs[1].KeyFingerprint);
  }

  void EnsureImported()
  {
    if(keys == null) T01_ImportTestKeys();
  }

  ExeGPG gpg;
  System.Security.SecureString password;
  Keyring keyring;
  PrimaryKey[] keys;
}

} // namespace AdamMil.Security.Tests