// Copyright (c) 2023 Poderosa Project, All Rights Reserved.
// This file is a part of the Granados SSH Client Library that is subject to
// the license included in the distributed package.
// You may not use this file except in compliance with the license.

using System;

using Granados.Mono.Math;

namespace Granados.DH {

    /// <summary>
    /// Diffie-Hellman specified in RFC4419
    /// </summary>
    public class DiffieHellman {

        private readonly BigInteger _p; // prime
        private readonly BigInteger _x; // random number
        private readonly BigInteger _e; // g^x mod p

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="algorithm">key exchange algorithm</param>
        /// <exception cref="DiffieHellmanException">unknown key exchange algorithm</exception>
        public DiffieHellman(KexAlgorithm algorithm) {
            _p = GetDiffieHellmanPrime(algorithm);
            //Generate x : 1 < x < (p-1)/2
            int xBytes = (_p.BitCount() - 2) / 8;
            BigInteger x;
            Rng rng = RngManager.GetSecureRng();
            do {
                byte[] sx = new byte[xBytes];
                rng.GetBytes(sx);
                x = new BigInteger(sx);
            } while (x <= 1);
            _x = x;
            _e = new BigInteger(2).ModPow(x, _p);
        }

        /// <summary>
        /// g^x mod p
        /// </summary>
        public BigInteger GPowXModP {
            get {
                return _e;
            }
        }

        /// <summary>
        /// Calculate a shared secret from g^y mod p
        /// </summary>
        /// <param name="f">g^y mod p</param>
        /// <returns>a shared secret</returns>
        public BigInteger CalculateSecret(BigInteger f) {
            return f.ModPow(_x, _p);
        }

        /// <summary>
        /// Gets a prime number for the Diffie-Hellman key exchange.
        /// </summary>
        /// <param name="algorithm">key exchange algorithm</param>
        /// <returns>a prime number</returns>
        private static BigInteger GetDiffieHellmanPrime(KexAlgorithm algorithm) {
            switch (algorithm) {
                case KexAlgorithm.DH_G1_SHA1:
                    return _dh_g1_prime.Value;

                case KexAlgorithm.DH_G14_SHA1:
                case KexAlgorithm.DH_G14_SHA256:
                    return _dh_g14_prime.Value;

                case KexAlgorithm.DH_G16_SHA512:
                    return _dh_g16_prime.Value;

                case KexAlgorithm.DH_G18_SHA512:
                    return _dh_g18_prime.Value;

                default:
                    throw new DiffieHellmanException("unknwon KexAlgorithm : " + algorithm.ToString());
            }
        }

        private static readonly Lazy<BigInteger> _dh_g1_prime = new Lazy<BigInteger>(
            () => new BigInteger(new uint[] {
                // RFC2409 1024-bit MODP Group 2
                // FFFFFFFFFFFFFFFFC90FDAA22168C234C4C6628B80DC1CD1
                // 29024E088A67CC74020BBEA63B139B22514A08798E3404DD
                // EF9519B3CD3A431B302B0A6DF25F14374FE1356D6D51C245
                // E485B576625E7EC6F44C42E9A637ED6B0BFF5CB6F406B7ED
                // EE386BFB5A899FA5AE9F24117C4B1FE649286651ECE65381
                // FFFFFFFFFFFFFFFF
                0xffffffffu, 0xffffffffu, 0xc90fdaa2u, 0x2168c234u,
                0xc4c6628bu, 0x80dc1cd1u, 0x29024e08u, 0x8a67cc74u,
                0x020bbea6u, 0x3b139b22u, 0x514a0879u, 0x8e3404ddu,
                0xef9519b3u, 0xcd3a431bu, 0x302b0a6du, 0xf25f1437u,
                0x4fe1356du, 0x6d51c245u, 0xe485b576u, 0x625e7ec6u,
                0xf44c42e9u, 0xa637ed6bu, 0x0bff5cb6u, 0xf406b7edu,
                0xee386bfbu, 0x5a899fa5u, 0xae9f2411u, 0x7c4b1fe6u,
                0x49286651u, 0xece65381u, 0xffffffffu, 0xffffffffu,
            })
        );

        private static readonly Lazy<BigInteger> _dh_g14_prime = new Lazy<BigInteger>(
            () => new BigInteger(new uint[] {
                // RFC3526 2048-bit MODP Group 14
                // FFFFFFFFFFFFFFFFC90FDAA22168C234C4C6628B80DC1CD1
                // 29024E088A67CC74020BBEA63B139B22514A08798E3404DD
                // EF9519B3CD3A431B302B0A6DF25F14374FE1356D6D51C245
                // E485B576625E7EC6F44C42E9A637ED6B0BFF5CB6F406B7ED
                // EE386BFB5A899FA5AE9F24117C4B1FE649286651ECE45B3D
                // C2007CB8A163BF0598DA48361C55D39A69163FA8FD24CF5F
                // 83655D23DCA3AD961C62F356208552BB9ED529077096966D
                // 670C354E4ABC9804F1746C08CA18217C32905E462E36CE3B
                // E39E772C180E86039B2783A2EC07A28FB5C55DF06F4C52C9
                // DE2BCBF6955817183995497CEA956AE515D2261898FA0510
                // 15728E5A8AACAA68FFFFFFFFFFFFFFFF
                0xffffffffu, 0xffffffffu, 0xc90fdaa2u, 0x2168c234u,
                0xc4c6628bu, 0x80dc1cd1u, 0x29024e08u, 0x8a67cc74u,
                0x020bbea6u, 0x3b139b22u, 0x514a0879u, 0x8e3404ddu,
                0xef9519b3u, 0xcd3a431bu, 0x302b0a6du, 0xf25f1437u,
                0x4fe1356du, 0x6d51c245u, 0xe485b576u, 0x625e7ec6u,
                0xf44c42e9u, 0xa637ed6bu, 0x0bff5cb6u, 0xf406b7edu,
                0xee386bfbu, 0x5a899fa5u, 0xae9f2411u, 0x7c4b1fe6u,
                0x49286651u, 0xece45b3du, 0xc2007cb8u, 0xa163bf05u,
                0x98da4836u, 0x1c55d39au, 0x69163fa8u, 0xfd24cf5fu,
                0x83655d23u, 0xdca3ad96u, 0x1c62f356u, 0x208552bbu,
                0x9ed52907u, 0x7096966du, 0x670c354eu, 0x4abc9804u,
                0xf1746c08u, 0xca18217cu, 0x32905e46u, 0x2e36ce3bu,
                0xe39e772cu, 0x180e8603u, 0x9b2783a2u, 0xec07a28fu,
                0xb5c55df0u, 0x6f4c52c9u, 0xde2bcbf6u, 0x95581718u,
                0x3995497cu, 0xea956ae5u, 0x15d22618u, 0x98fa0510u,
                0x15728e5au, 0x8aacaa68u, 0xffffffffu, 0xffffffffu,
            })
        );

        private static readonly Lazy<BigInteger> _dh_g16_prime = new Lazy<BigInteger>(
            () => new BigInteger(new uint[] {
                // RFC3526 4096-bit MODP Group 16
                // FFFFFFFFFFFFFFFFC90FDAA22168C234C4C6628B80DC1CD1
                // 29024E088A67CC74020BBEA63B139B22514A08798E3404DD
                // EF9519B3CD3A431B302B0A6DF25F14374FE1356D6D51C245
                // E485B576625E7EC6F44C42E9A637ED6B0BFF5CB6F406B7ED
                // EE386BFB5A899FA5AE9F24117C4B1FE649286651ECE45B3D
                // C2007CB8A163BF0598DA48361C55D39A69163FA8FD24CF5F
                // 83655D23DCA3AD961C62F356208552BB9ED529077096966D
                // 670C354E4ABC9804F1746C08CA18217C32905E462E36CE3B
                // E39E772C180E86039B2783A2EC07A28FB5C55DF06F4C52C9
                // DE2BCBF6955817183995497CEA956AE515D2261898FA0510
                // 15728E5A8AAAC42DAD33170D04507A33A85521ABDF1CBA64
                // ECFB850458DBEF0A8AEA71575D060C7DB3970F85A6E1E4C7
                // ABF5AE8CDB0933D71E8C94E04A25619DCEE3D2261AD2EE6B
                // F12FFA06D98A0864D87602733EC86A64521F2B18177B200C
                // BBE117577A615D6C770988C0BAD946E208E24FA074E5AB31
                // 43DB5BFCE0FD108E4B82D120A92108011A723C12A787E6D7
                // 88719A10BDBA5B2699C327186AF4E23C1A946834B6150BDA
                // 2583E9CA2AD44CE8DBBBC2DB04DE8EF92E8EFC141FBECAA6
                // 287C59474E6BC05D99B2964FA090C3A2233BA186515BE7ED
                // 1F612970CEE2D7AFB81BDD762170481CD0069127D5B05AA9
                // 93B4EA988D8FDDC186FFB7DC90A6C08F4DF435C934063199
                // FFFFFFFFFFFFFFFF
                0xffffffffu, 0xffffffffu, 0xc90fdaa2u, 0x2168c234u,
                0xc4c6628bu, 0x80dc1cd1u, 0x29024e08u, 0x8a67cc74u,
                0x020bbea6u, 0x3b139b22u, 0x514a0879u, 0x8e3404ddu,
                0xef9519b3u, 0xcd3a431bu, 0x302b0a6du, 0xf25f1437u,
                0x4fe1356du, 0x6d51c245u, 0xe485b576u, 0x625e7ec6u,
                0xf44c42e9u, 0xa637ed6bu, 0x0bff5cb6u, 0xf406b7edu,
                0xee386bfbu, 0x5a899fa5u, 0xae9f2411u, 0x7c4b1fe6u,
                0x49286651u, 0xece45b3du, 0xc2007cb8u, 0xa163bf05u,
                0x98da4836u, 0x1c55d39au, 0x69163fa8u, 0xfd24cf5fu,
                0x83655d23u, 0xdca3ad96u, 0x1c62f356u, 0x208552bbu,
                0x9ed52907u, 0x7096966du, 0x670c354eu, 0x4abc9804u,
                0xf1746c08u, 0xca18217cu, 0x32905e46u, 0x2e36ce3bu,
                0xe39e772cu, 0x180e8603u, 0x9b2783a2u, 0xec07a28fu,
                0xb5c55df0u, 0x6f4c52c9u, 0xde2bcbf6u, 0x95581718u,
                0x3995497cu, 0xea956ae5u, 0x15d22618u, 0x98fa0510u,
                0x15728e5au, 0x8aaac42du, 0xad33170du, 0x04507a33u,
                0xa85521abu, 0xdf1cba64u, 0xecfb8504u, 0x58dbef0au,
                0x8aea7157u, 0x5d060c7du, 0xb3970f85u, 0xa6e1e4c7u,
                0xabf5ae8cu, 0xdb0933d7u, 0x1e8c94e0u, 0x4a25619du,
                0xcee3d226u, 0x1ad2ee6bu, 0xf12ffa06u, 0xd98a0864u,
                0xd8760273u, 0x3ec86a64u, 0x521f2b18u, 0x177b200cu,
                0xbbe11757u, 0x7a615d6cu, 0x770988c0u, 0xbad946e2u,
                0x08e24fa0u, 0x74e5ab31u, 0x43db5bfcu, 0xe0fd108eu,
                0x4b82d120u, 0xa9210801u, 0x1a723c12u, 0xa787e6d7u,
                0x88719a10u, 0xbdba5b26u, 0x99c32718u, 0x6af4e23cu,
                0x1a946834u, 0xb6150bdau, 0x2583e9cau, 0x2ad44ce8u,
                0xdbbbc2dbu, 0x04de8ef9u, 0x2e8efc14u, 0x1fbecaa6u,
                0x287c5947u, 0x4e6bc05du, 0x99b2964fu, 0xa090c3a2u,
                0x233ba186u, 0x515be7edu, 0x1f612970u, 0xcee2d7afu,
                0xb81bdd76u, 0x2170481cu, 0xd0069127u, 0xd5b05aa9u,
                0x93b4ea98u, 0x8d8fddc1u, 0x86ffb7dcu, 0x90a6c08fu,
                0x4df435c9u, 0x34063199u, 0xffffffffu, 0xffffffffu,
            })
        );

        private static readonly Lazy<BigInteger> _dh_g18_prime = new Lazy<BigInteger>(
            () => new BigInteger(new uint[] {
                // RFC3526 8192-bit MODP Group 18
                // FFFFFFFFFFFFFFFFC90FDAA22168C234C4C6628B80DC1CD1
                // 29024E088A67CC74020BBEA63B139B22514A08798E3404DD
                // EF9519B3CD3A431B302B0A6DF25F14374FE1356D6D51C245
                // E485B576625E7EC6F44C42E9A637ED6B0BFF5CB6F406B7ED
                // EE386BFB5A899FA5AE9F24117C4B1FE649286651ECE45B3D
                // C2007CB8A163BF0598DA48361C55D39A69163FA8FD24CF5F
                // 83655D23DCA3AD961C62F356208552BB9ED529077096966D
                // 670C354E4ABC9804F1746C08CA18217C32905E462E36CE3B
                // E39E772C180E86039B2783A2EC07A28FB5C55DF06F4C52C9
                // DE2BCBF6955817183995497CEA956AE515D2261898FA0510
                // 15728E5A8AAAC42DAD33170D04507A33A85521ABDF1CBA64
                // ECFB850458DBEF0A8AEA71575D060C7DB3970F85A6E1E4C7
                // ABF5AE8CDB0933D71E8C94E04A25619DCEE3D2261AD2EE6B
                // F12FFA06D98A0864D87602733EC86A64521F2B18177B200C
                // BBE117577A615D6C770988C0BAD946E208E24FA074E5AB31
                // 43DB5BFCE0FD108E4B82D120A92108011A723C12A787E6D7
                // 88719A10BDBA5B2699C327186AF4E23C1A946834B6150BDA
                // 2583E9CA2AD44CE8DBBBC2DB04DE8EF92E8EFC141FBECAA6
                // 287C59474E6BC05D99B2964FA090C3A2233BA186515BE7ED
                // 1F612970CEE2D7AFB81BDD762170481CD0069127D5B05AA9
                // 93B4EA988D8FDDC186FFB7DC90A6C08F4DF435C934028492
                // 36C3FAB4D27C7026C1D4DCB2602646DEC9751E763DBA37BD
                // F8FF9406AD9E530EE5DB382F413001AEB06A53ED9027D831
                // 179727B0865A8918DA3EDBEBCF9B14ED44CE6CBACED4BB1B
                // DB7F1447E6CC254B332051512BD7AF426FB8F401378CD2BF
                // 5983CA01C64B92ECF032EA15D1721D03F482D7CE6E74FEF6
                // D55E702F46980C82B5A84031900B1C9E59E7C97FBEC7E8F3
                // 23A97A7E36CC88BE0F1D45B7FF585AC54BD407B22B4154AA
                // CC8F6D7EBF48E1D814CC5ED20F8037E0A79715EEF29BE328
                // 06A1D58BB7C5DA76F550AA3D8A1FBFF0EB19CCB1A313D55C
                // DA56C9EC2EF29632387FE8D76E3C0468043E8F663F4860EE
                // 12BF2D5B0B7474D6E694F91E6DBE115974A3926F12FEE5E4
                // 38777CB6A932DF8CD8BEC4D073B931BA3BC832B68D9DD300
                // 741FA7BF8AFC47ED2576F6936BA424663AAB639C5AE4F568
                // 3423B4742BF1C978238F16CBE39D652DE3FDB8BEFC848AD9
                // 22222E04A4037C0713EB57A81A23F0C73473FC646CEA306B
                // 4BCBC8862F8385DDFA9D4B7FA2C087E879683303ED5BDD3A
                // 062B3CF5B3A278A66D2A13F83F44F82DDF310EE074AB6A36
                // 4597E899A0255DC164F31CC50846851DF9AB48195DED7EA1
                // B1D510BD7EE74D73FAF36BC31ECFA268359046F4EB879F92
                // 4009438B481C6CD7889A002ED5EE382BC9190DA6FC026E47
                // 9558E4475677E9AA9E3050E2765694DFC81F56E880B96E71
                // 60C980DD98EDD3DFFFFFFFFFFFFFFFFF
                0xffffffffu, 0xffffffffu, 0xc90fdaa2u, 0x2168c234u,
                0xc4c6628bu, 0x80dc1cd1u, 0x29024e08u, 0x8a67cc74u,
                0x020bbea6u, 0x3b139b22u, 0x514a0879u, 0x8e3404ddu,
                0xef9519b3u, 0xcd3a431bu, 0x302b0a6du, 0xf25f1437u,
                0x4fe1356du, 0x6d51c245u, 0xe485b576u, 0x625e7ec6u,
                0xf44c42e9u, 0xa637ed6bu, 0x0bff5cb6u, 0xf406b7edu,
                0xee386bfbu, 0x5a899fa5u, 0xae9f2411u, 0x7c4b1fe6u,
                0x49286651u, 0xece45b3du, 0xc2007cb8u, 0xa163bf05u,
                0x98da4836u, 0x1c55d39au, 0x69163fa8u, 0xfd24cf5fu,
                0x83655d23u, 0xdca3ad96u, 0x1c62f356u, 0x208552bbu,
                0x9ed52907u, 0x7096966du, 0x670c354eu, 0x4abc9804u,
                0xf1746c08u, 0xca18217cu, 0x32905e46u, 0x2e36ce3bu,
                0xe39e772cu, 0x180e8603u, 0x9b2783a2u, 0xec07a28fu,
                0xb5c55df0u, 0x6f4c52c9u, 0xde2bcbf6u, 0x95581718u,
                0x3995497cu, 0xea956ae5u, 0x15d22618u, 0x98fa0510u,
                0x15728e5au, 0x8aaac42du, 0xad33170du, 0x04507a33u,
                0xa85521abu, 0xdf1cba64u, 0xecfb8504u, 0x58dbef0au,
                0x8aea7157u, 0x5d060c7du, 0xb3970f85u, 0xa6e1e4c7u,
                0xabf5ae8cu, 0xdb0933d7u, 0x1e8c94e0u, 0x4a25619du,
                0xcee3d226u, 0x1ad2ee6bu, 0xf12ffa06u, 0xd98a0864u,
                0xd8760273u, 0x3ec86a64u, 0x521f2b18u, 0x177b200cu,
                0xbbe11757u, 0x7a615d6cu, 0x770988c0u, 0xbad946e2u,
                0x08e24fa0u, 0x74e5ab31u, 0x43db5bfcu, 0xe0fd108eu,
                0x4b82d120u, 0xa9210801u, 0x1a723c12u, 0xa787e6d7u,
                0x88719a10u, 0xbdba5b26u, 0x99c32718u, 0x6af4e23cu,
                0x1a946834u, 0xb6150bdau, 0x2583e9cau, 0x2ad44ce8u,
                0xdbbbc2dbu, 0x04de8ef9u, 0x2e8efc14u, 0x1fbecaa6u,
                0x287c5947u, 0x4e6bc05du, 0x99b2964fu, 0xa090c3a2u,
                0x233ba186u, 0x515be7edu, 0x1f612970u, 0xcee2d7afu,
                0xb81bdd76u, 0x2170481cu, 0xd0069127u, 0xd5b05aa9u,
                0x93b4ea98u, 0x8d8fddc1u, 0x86ffb7dcu, 0x90a6c08fu,
                0x4df435c9u, 0x34028492u, 0x36c3fab4u, 0xd27c7026u,
                0xc1d4dcb2u, 0x602646deu, 0xc9751e76u, 0x3dba37bdu,
                0xf8ff9406u, 0xad9e530eu, 0xe5db382fu, 0x413001aeu,
                0xb06a53edu, 0x9027d831u, 0x179727b0u, 0x865a8918u,
                0xda3edbebu, 0xcf9b14edu, 0x44ce6cbau, 0xced4bb1bu,
                0xdb7f1447u, 0xe6cc254bu, 0x33205151u, 0x2bd7af42u,
                0x6fb8f401u, 0x378cd2bfu, 0x5983ca01u, 0xc64b92ecu,
                0xf032ea15u, 0xd1721d03u, 0xf482d7ceu, 0x6e74fef6u,
                0xd55e702fu, 0x46980c82u, 0xb5a84031u, 0x900b1c9eu,
                0x59e7c97fu, 0xbec7e8f3u, 0x23a97a7eu, 0x36cc88beu,
                0x0f1d45b7u, 0xff585ac5u, 0x4bd407b2u, 0x2b4154aau,
                0xcc8f6d7eu, 0xbf48e1d8u, 0x14cc5ed2u, 0x0f8037e0u,
                0xa79715eeu, 0xf29be328u, 0x06a1d58bu, 0xb7c5da76u,
                0xf550aa3du, 0x8a1fbff0u, 0xeb19ccb1u, 0xa313d55cu,
                0xda56c9ecu, 0x2ef29632u, 0x387fe8d7u, 0x6e3c0468u,
                0x043e8f66u, 0x3f4860eeu, 0x12bf2d5bu, 0x0b7474d6u,
                0xe694f91eu, 0x6dbe1159u, 0x74a3926fu, 0x12fee5e4u,
                0x38777cb6u, 0xa932df8cu, 0xd8bec4d0u, 0x73b931bau,
                0x3bc832b6u, 0x8d9dd300u, 0x741fa7bfu, 0x8afc47edu,
                0x2576f693u, 0x6ba42466u, 0x3aab639cu, 0x5ae4f568u,
                0x3423b474u, 0x2bf1c978u, 0x238f16cbu, 0xe39d652du,
                0xe3fdb8beu, 0xfc848ad9u, 0x22222e04u, 0xa4037c07u,
                0x13eb57a8u, 0x1a23f0c7u, 0x3473fc64u, 0x6cea306bu,
                0x4bcbc886u, 0x2f8385ddu, 0xfa9d4b7fu, 0xa2c087e8u,
                0x79683303u, 0xed5bdd3au, 0x062b3cf5u, 0xb3a278a6u,
                0x6d2a13f8u, 0x3f44f82du, 0xdf310ee0u, 0x74ab6a36u,
                0x4597e899u, 0xa0255dc1u, 0x64f31cc5u, 0x0846851du,
                0xf9ab4819u, 0x5ded7ea1u, 0xb1d510bdu, 0x7ee74d73u,
                0xfaf36bc3u, 0x1ecfa268u, 0x359046f4u, 0xeb879f92u,
                0x4009438bu, 0x481c6cd7u, 0x889a002eu, 0xd5ee382bu,
                0xc9190da6u, 0xfc026e47u, 0x9558e447u, 0x5677e9aau,
                0x9e3050e2u, 0x765694dfu, 0xc81f56e8u, 0x80b96e71u,
                0x60c980ddu, 0x98edd3dfu, 0xffffffffu, 0xffffffffu,
            })
        );
    }

    /// <summary>
    /// Error in <see cref="DiffieHellman"/>.
    /// </summary>
    public class DiffieHellmanException : Exception {
        internal DiffieHellmanException(string message)
            : base(message) {
        }
    }
}
