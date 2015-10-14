﻿using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.IO;
using System.Text;
using System.Linq;


namespace XmlSaver {
    using ExDictionary = Dictionary<Type, Dictionary<string, object>>;

    [Serializable]
    public sealed class SaveDataElement {
        public string key { get; private set; }
        public object value { get; private set; }
        public string type { get; private set; }
        public Type ValueType { get { return Type.GetType(this.type); } }


        public SaveDataElement() : this(Guid.NewGuid().ToString(), new object(), typeof(object).FullName) { ; }
        public SaveDataElement(string key, object value, string type) { this.Set(key, value, type); }
        public SaveDataElement(string key, object value, Type type) { this.Set(key, value, type.FullName); }
        
        public void Set(string key, object value, string type) {
            if(key == null) { throw new ArgumentNullException("key", "Key cannot be null."); }
            if(key == "") { throw new ArgumentException("key", "Key cannot be empty."); }

            if(value == null) { throw new ArgumentNullException("value", "Value cannot be null."); }

            if(type == null) { throw new ArgumentNullException("type", "Type cannnot be null."); }
            if(type == "") { throw new ArgumentException("type", "Type cannot be empty."); }

            this.key = key;
            this.value = value;
            this.type = type;
        }
    }

    public sealed class XmlSaver {
        public static string FileName {
            get { return fileName; }
            set { if(value != null && value != "") { fileName = value.EndsWith(extension) ? value : value + extension; } }
        }
        public static string Extension {
            get { return extension; }
            set { if(value != null && value != "") { extension = value.StartsWith(".") ? value : "." + value; } }
        }
        public static string FileNameWithoutExtension { get { return FileName.Substring(0, FileName.Length - Extension.Length); } }
        public static string FullPath { get { return Application.persistentDataPath + Path.DirectorySeparatorChar + FileName; } }

        private static string fileName = "XmlSaver.xml";
        private static string extension = ".xml";
        private static ExDictionary dictionary = new ExDictionary();
        private readonly static XmlSerializer serializer = new XmlSerializer(typeof(List<SaveDataElement>));
        private readonly static UTF8Encoding encode = new UTF8Encoding(false);


        static XmlSaver() {
            dictionary = Load();
        }

        public static void DeleteAll() {
            dictionary.Clear();
            Save();
        }
        
        public static void DeleteKey(string key) {
            foreach(var pair in dictionary) { pair.Value.Remove(key); }

            Save();
        }
        
        public static void DeleteKey(string key, Type type) {
            dictionary[type].Remove(key);
            Save();
        }

        public static void Save() {
            if(dictionary.Count <= 0) {
                File.Delete(FullPath);
                return;
            }

            using(var sw = new StreamWriter(FullPath, false, encode)) {
                serializer.Serialize(sw, ConvertExDictionary2List(dictionary));
            }
        }

        public static bool HasKey(string key) {
            foreach(var pair in dictionary) {
                if(HasKey(key, pair.Key)) { return true; }
            }

            return false;
        }

        public static bool HasKey(string key, Type type) {
            return dictionary.ContainsKey(type) && dictionary[type].ContainsKey(key);
        }

        #region "Setters"
        public static void Set<T>(string key, T value) {
            var serializer = new XmlSerializer(typeof(T));
            
            using(var sw = new StringWriter()) {
                serializer.Serialize(sw, value);
                SetValue(key, sw.ToString(), typeof(T));
            }
        }

        public static void SetFloat(string key, float value) {
            SetValue<float>(key, value);
        }

        public static void SetInt(string key, int value) {
            SetValue<int>(key, value);
        }

        public static void SetString(string key, string value) {
            SetValue<string>(key, value);
        }

        public static void SetBool(string key, bool value) {
            SetValue<bool>(key, value);
        }

        private static void SetValue<T>(string key, T value, Type type = null) {
            type = (type == null ? typeof(T) : type);
            if(!dictionary.ContainsKey(type)) { dictionary[type] = new Dictionary<string, object>(); }

            dictionary[type][key] = value;
        }
        #endregion

        #region "Getters"
        public static T Get<T>(string key) {
            return Get<T>(key, default(T));
        }

        public static T Get<T>(string key, T defaultValue = default(T)) {
            return Get<T>(key, defaultValue, obj => {
                var serializer = new XmlSerializer(typeof(T));

                using(var sr = new StringReader((string)obj)) {
                    return (T)serializer.Deserialize(sr);
                }
            });
        }

        public static float GetFloat(string key, float defaultValue = default(float)) {
            return Get<float>(key, defaultValue, null);
        }

        public static float GetInt(string key, int defaultValue = default(int)) {
            return Get<int>(key, defaultValue, null);
        }

        public static string GetString(string key, string defaultValue = "") {
            return Get<string>(key, defaultValue, null);
        }

        public static bool GetBool(string key, bool defaultValue = default(bool)) {
            return Get<bool>(key, defaultValue, null);
        }

        private static T Get<T>(string key, T defaultValue, Func<object, T> getter) {
            var type = typeof(T);
            return HasKey(key, type) ? (getter == null ? (T)dictionary[type][key] : getter(dictionary[type][key])) : defaultValue;
        }
        #endregion
        
        private static ExDictionary Load() {
            if(!File.Exists(FullPath)) { return new ExDictionary(); }
            
            using(var sr = new StreamReader(FullPath, encode)) {
                return ConvertList2ExDictionary((List<SaveDataElement>)serializer.Deserialize(sr));
            }
        }

        private static ExDictionary ConvertList2ExDictionary(List<SaveDataElement> list) {
            dictionary = new Dictionary<Type, Dictionary<string, object>>();
            list.ForEach(e => SetValue(e.key, e.value, e.ValueType));

            return dictionary;
        }

        private static List<SaveDataElement> ConvertExDictionary2List(ExDictionary dic) {
            var list = new List<SaveDataElement>();
            foreach(var pair in dic) {
                foreach(var e in pair.Value) { list.Add(new SaveDataElement(e.Key, e.Value, pair.Key)); }
            }

            return list;
        }
    }
}
