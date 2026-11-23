using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
namespace OceanAngler
{
    [Serializable] sealed class SaveEnvelope { public int version = 1; public string payload, checksum; }
    public sealed class SaveLoadResult
    {
        public ProgressData data;
        public string message;
        public bool requiresRecovery, recoveredBackup;
    }
    // 写入成功后才发布内存状态；主文件和备份都不能读时必须经家长区显式恢复。
    public sealed class SaveStore
    {
        public string PathName { get; }
        readonly ContentCatalog catalog;
        public SaveStore(string path, ContentCatalog content) { PathName = path; catalog = content; }
        public SaveLoadResult Load()
        {
            if (!File.Exists(PathName) && !File.Exists(PathName + ".bak")) return new SaveLoadResult { data = new ProgressData() };
            if (File.Exists(PathName))
            {
                try { return new SaveLoadResult { data = Read(PathName) }; }
                catch (FutureSaveException) { return Recovery("存档来自较新的游戏版本，请更新游戏。原文件已保留。"); }
                catch (Exception e) when (e is IOException || e is InvalidOperationException || e is ArgumentException) { }
            }
            try
            {
                if (File.Exists(PathName + ".bak")) return new SaveLoadResult { data = Read(PathName + ".bak"), recoveredBackup = true, message = "已读取上一份完整存档。原文件已保留，可继续游戏。" };
            }
            catch (FutureSaveException) { return Recovery("备份来自较新的游戏版本，请更新游戏。原文件已保留。"); }
            catch (Exception e) when (e is IOException || e is InvalidOperationException || e is ArgumentException) { }
            return Recovery("存档暂时无法读取，原文件已保留。请在家长区恢复。");
        }
        static SaveLoadResult Recovery(string message) => new SaveLoadResult { requiresRecovery = true, message = message };
        ProgressData Read(string path)
        {
            var envelope = JsonUtility.FromJson<SaveEnvelope>(File.ReadAllText(path));
            if (envelope != null && envelope.version > 1) throw new FutureSaveException();
            if (envelope == null || envelope.version != 1 || string.IsNullOrEmpty(envelope.payload) || envelope.checksum != Hash(envelope.payload)) throw new InvalidOperationException("Invalid envelope");
            var p = JsonUtility.FromJson<ProgressData>(envelope.payload);
            if (p != null && p.schemaVersion > 1) throw new FutureSaveException();
            if (p == null) throw new InvalidOperationException("Missing payload"); p.Validate(catalog); return p;
        }
        public void Write(ProgressData p)
        {
            p.Validate(catalog);
            var directory = Path.GetDirectoryName(PathName); if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
            string temp = PathName + ".tmp", backup = PathName + ".bak";
            string payload = JsonUtility.ToJson(p);
            var bytes = Encoding.UTF8.GetBytes(JsonUtility.ToJson(new SaveEnvelope { payload = payload, checksum = Hash(payload) }));
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
        public void ResetPreservingOriginals()
        {
            foreach (var suffix in new[] { "", ".bak", ".tmp" })
                if (File.Exists(PathName + suffix)) File.Move(PathName + suffix, QuarantineName());
            Write(new ProgressData());
        }
        static string Hash(string value)
        {
            using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(value))).Replace("-", "");
        }
        sealed class FutureSaveException : Exception { }
    }
}
