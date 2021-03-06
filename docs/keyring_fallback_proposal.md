# Proposal for Linux plain-text file fallback

**Context:** Today, the MSAL extension libraries (.net, java, python, javascript) store secrets in KeyRings via LibSecret. These components not available on all Linux distros and they cannot be started when connected via SSH.

**Proposal:** Extensions are to provide a mechanism for products to detect if this secret storage is usable. If it is not, extensions are to write the token cache to an unecrypted file. It then becomes the reponsability of the Linux users to protect their files, using, for example, encrypted disks.
l
#### Current API

```csharp
 var storageProperties =
    // CacheFileName is used for storgae only on Win. On Mac and Linux, it's used 
    // to produce an event, but contents are empty. Actual secrets are stored in KeyRing 
    // KeyChain.
    new StorageCreationPropertiesBuilder(Config.CacheFileName, Config.CacheDir, Config.ClientId)
        .WithLinuxKeyring(
            Config.LinuxKeyRingSchema,
            Config.LinuxKeyRingCollection,
            Config.LinuxKeyRingLabel,
            Config.LinuxKeyRingAttr1,
            Config.LinuxKeyRingAttr2)
        .WithMacKeyChain(
            Config.KeyChainServiceName,
            Config.KeyChainAccountName)
        .Build();

MsalCacheHelper cacheHelper = MsalCacheHelper.Create(storageProperties);
cacheHelper.RegisterCache(app.UserTokenCache);

```
## Goals

1. API for fallback to file for Linux. (P0)

2. Developers must opt-in to fallback, it should not be a default, since fallback is insecure. (P0)

3. API for detecting if persistence is not working. This will allow products to show users a warning message about the fallback. (P1)

4. If a user connects both via SSH and via UI, her SSH token cache (i.e. the plaintext file) should not be deleted. (P2)

### Non-goals
1. We do not plan to support multiple token cache sources. Token cache is read either from file or from KeyRing. No merging mechanisms exist.
2. Mechanism is not supposed to work on Windows and Mac. Encryption on Windows and Mac via current mechanisms (DPAPI / KeyChain) is guaranteed by the OS.

## Proposal

#### Add a method to check persistence on Linux

```csharp
void cacheHelper.VerifyPersistence();
```

This method MUST not affect the token cache. It will attempt to write and read a dummy secret. Different storage attributes will be used so as to not interfere with the real token cache (e.g. Windows - different file path, Mac - different account, Linux - differnt keyring attribute)

If this method fails it throws an exception with more details. Typically the failure points are:

- LibSecret is not installed
- Incorrect version of LibSecret is installed
- D-BUS is not running (typical in SSH scenario)
- No wallet is listening on the other end

#### Add a method to persist data in a plaintext file


```csharp
 new StorageCreationPropertiesBuilder(Config.CacheFileName, Config.CacheDir, Config.ClientId) 
    .WithLinuxUnprotectedFile() //new method                     
    .WithMacKeyChain(...); // no change
                     
```                     
`Config.CacheFileName` will contain the unprotected cache. 

Note: `WithLinuxUnprotectedFile` cannot be used in conjuction with `WithLinuxKeyring` - an exception will be thrown

#### Suggested pattern for extension consumers
 
Libraries consuming the extension will: 

1. create a cache helper with a the normal `KeyRing` setup
2. call `cacheHelper.VerifyPersistence()`
3. If this throws an exception, show the user a meaningful message / URL to help page to inform them to secure their secrets storage
4. Create a cache helper using `.WithLinuxUnprotectedFile` using a file path that comes from either: 
- a well known env variable, e.g. LINUX_DEV_TOOLS_TOKEN_CACHE
- if LINUX_DEV_TOOLS_TOKEN_CACHE is not set, default to a well known location 


#### Important note about signaling API

Some consumenrs of the library are using the event `CacheChanged`. While not all extensions expose this event, all extension need to ensure the event is triggered. 
This is done via a `FileWatcher` mechanism as follows:

- on Windows, encrypted data is persisted to `Config.CacheFileName` 
- on Mac, data is stored in KeyChain. A dummy 1 byte is written to `Config.CacheFileName`
- on Linux, data is persisted EITHER in KeyRin or in `Config.CacheFileName`. To maintain the signaling semantics, a dummy 1 byte will be written to a file named
`Config.CacheFileName` + '.signal'