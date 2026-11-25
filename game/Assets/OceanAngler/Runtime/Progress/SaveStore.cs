using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
namespace OceanAngler
{
    [Serializable] sealed class SaveEnvelope { public int version = 1; public string payload, checksum, profile; public bool developerSelected; }
    public sealed class SaveLoadResult
    {
        public ProgressData data;
        public string message;
        public bool requiresRecovery, recoveredBackup, developerSelected;
    }
    // 写入成功后才发布内存状态；主文件和备份都不能读时必须经家长区显式恢复。
    public sealed class SaveStore
    {
        public string PathName { get; }
        public bool DeveloperSandbox { get; }
        readonly ContentCatalog catalog;
        public SaveStore(string path, ContentCatalog content, bool developerSandbox = false) { PathName = path; catalog = content; DeveloperSandbox=developerSandbox; }
        public bool HasExisting => File.Exists(PathName)||File.Exists(PathName+".bak");
        public ProgressData CreateData() => DeveloperSandbox?new ProgressData{schemaVersion=2,tutorialComplete=true}:new ProgressData();
        public SaveLoadResult Load()
        {
            if (!HasExisting) return new SaveLoadResult { data = CreateData() };
            if (File.Exists(PathName))
            {
                try { return Read(PathName); }
                catch (FutureSaveException) { return Recovery("存档来自较新的游戏版本，请更新游戏。原文件已保留。"); }
                catch (Exception e) when (e is IOException || e is InvalidOperationException || e is ArgumentException) { }
            }
            try
            {
                if (File.Exists(PathName + ".bak")) { var result=Read(PathName+".bak");result.recoveredBackup=true;result.message="已读取上一份完整存档。原文件已保留，可继续游戏。";return result; }
            }
            catch (FutureSaveException) { return Recovery("备份来自较新的游戏版本，请更新游戏。原文件已保留。"); }
            catch (Exception e) when (e is IOException || e is InvalidOperationException || e is ArgumentException) { }
            return Recovery("存档暂时无法读取，原文件已保留。请在家长区恢复。");
        }
        static SaveLoadResult Recovery(string message) => new SaveLoadResult { requiresRecovery = true, message = message };
        SaveLoadResult Read(string path)
        {
            var envelope = JsonUtility.FromJson<SaveEnvelope>(File.ReadAllText(path));
            int version=DeveloperSandbox?2:1;
            if (envelope != null && envelope.version > version) throw new FutureSaveException();
            if (envelope == null || envelope.version != version || string.IsNullOrEmpty(envelope.payload)) throw new InvalidOperationException("Invalid envelope");
            bool wrongProfile=DeveloperSandbox?envelope.profile!="developer":(!string.IsNullOrEmpty(envelope.profile)&&envelope.profile!="normal")||envelope.developerSelected;
            if(wrongProfile)throw new InvalidOperationException("Wrong save profile");
            if(envelope.checksum!=Checksum(envelope.payload,envelope.developerSelected))throw new InvalidOperationException("Invalid checksum");
            var p = JsonUtility.FromJson<ProgressData>(envelope.payload);
            if (p != null && p.schemaVersion > 2) throw new FutureSaveException();
            if (p == null) throw new InvalidOperationException("Missing payload");
            // Legacy saves omit this additive field. Keep version 1 until an
            // observation is committed, so old builds cannot erase new records.
            if(p.schemaVersion==1&&p.observations==null)p.observations=new System.Collections.Generic.List<string>();
            p.Validate(catalog,DeveloperSandbox); return new SaveLoadResult{data=p,developerSelected=DeveloperSandbox&&envelope.developerSelected};
        }
        public void Write(ProgressData p, bool developerSelected = false)
        {
            if(!DeveloperSandbox&&developerSelected)throw new InvalidOperationException("Normal save cannot select developer mode");
            p.Validate(catalog,DeveloperSandbox);
            var directory = Path.GetDirectoryName(PathName); if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
            string temp = PathName + ".tmp", backup = PathName + ".bak";
            string payload = JsonUtility.ToJson(p);
            var bytes = Encoding.UTF8.GetBytes(JsonUtility.ToJson(new SaveEnvelope { version=DeveloperSandbox?2:1,profile=DeveloperSandbox?"developer":"normal",developerSelected=developerSelected,payload = payload, checksum = Checksum(payload,developerSelected) }));
            using (var file = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None)) { file.Write(bytes, 0, bytes.Length); file.Flush(true); }
            // 无效主文件不得覆盖已经验证的有效备份；将其转存后提交。
            if (File.Exists(PathName))
            {
                bool valid;
                try { Read(PathName); valid = true; }
                catch (FutureSaveException) { File.Delete(temp); throw new InvalidOperationException("Newer save must not be overwritten"); }
                catch (Exception e) when (e is IOException || e is InvalidOperationException || e is ArgumentException) { valid = false; }
                if (valid) File.Replace(temp, PathName, backup);
                else { File.Move(PathName, QuarantineName()); File.Move(temp, PathName); }
            }
            else File.Move(temp, PathName);
        }
        string QuarantineName() => PathName + ".preserved-" + Guid.NewGuid().ToString("N");
        public void ResetPreservingOriginals(bool developerSelected = false)
        {
            foreach (var suffix in new[] { "", ".bak", ".tmp" })
                if (File.Exists(PathName + suffix)) File.Move(PathName + suffix, QuarantineName());
            Write(CreateData(),developerSelected);
        }
        string Checksum(string payload,bool selected) => Hash(DeveloperSandbox?"developer\n"+(selected?"1":"0")+"\n"+payload:payload);
        static string Hash(string value)
        {
            using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(value))).Replace("-", "");
        }
        sealed class FutureSaveException : Exception { }
    }
}
