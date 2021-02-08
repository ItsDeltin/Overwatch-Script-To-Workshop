using static Deltin.Deltinteger.I18n.Keyword;

namespace Deltin.Deltinteger.Elements
{
    class VariableSet
    {
        public WorkshopVariable[] GlobalVariables { get; }
        public WorkshopVariable[] PlayerVariables { get; }

        public VariableSet(WorkshopVariable[] globalVariables, WorkshopVariable[] playerVariables)
        {
            GlobalVariables = globalVariables;
            PlayerVariables = playerVariables;
        }

        public void ToWorkshop(WorkshopBuilder builder)
        {
            builder.AppendKeywordLine(KEYWORD_VARIABLES);
            builder.AppendLine("{");
            builder.Indent();
            builder.AppendKeyword(KEYWORD_GLOBAL); builder.Append(":"); builder.AppendLine();
            builder.Indent();
            WriteCollection(builder, GlobalVariables);
            builder.Outdent();

            builder.AppendKeyword(KEYWORD_PLAYER); builder.Append(":"); builder.AppendLine();
            builder.Indent();
            WriteCollection(builder, PlayerVariables);
            builder.Outdent();
            builder.Outdent();
            builder.AppendLine("}");
        }

        static void WriteCollection(WorkshopBuilder builder, WorkshopVariable[] collection)
        {
            foreach (var var in collection)
                builder.AppendLine(var.ID + ": " + var.Name);
        }
    }

    class SubroutineSet
    {
        public Subroutine[] Subroutines { get; }

        public SubroutineSet(Subroutine[] subroutines)
        {
            Subroutines = subroutines;
        }

        public void ToWorkshop(WorkshopBuilder builder)
        {
            if (Subroutines.Length == 0) return;

            builder.AppendKeywordLine(KEYWORD_SUBROUTINES);
            builder.AppendLine("{");
            builder.Indent();

            foreach (Subroutine routine in Subroutines)
                builder.AppendLine(routine.ID.ToString() + ": " + routine.Name);

            builder.Outdent();
            builder.AppendLine("}");
            builder.AppendLine();
        }
    }
}