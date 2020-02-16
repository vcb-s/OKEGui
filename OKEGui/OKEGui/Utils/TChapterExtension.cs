using TChapter.Chapters;

namespace OKEGui.Utils
{
    static class TChapterExtension
    {
        public static void Save(this ChapterInfo info, ChapterTypeEnum chapterType, string savePath, int index = 0,
            bool removeName = false, string language = "", string sourceFileName = "")
        {
            new MultiChapterData(ChapterTypeEnum.UNKNOWN) {info}
                .Save(chapterType, savePath, index, removeName, language, sourceFileName);
        }
    }
}
