using UnityEditor;
using UnityEditor.Experimental.AssetImporters;

    [CustomEditor(typeof(headImporter))]
    class HeadImporterInspector : ScriptedImporterEditor
    {
        protected override bool useAssetDrawPreview => false;
    }