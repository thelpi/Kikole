using System;
using System.Data;
using Dapper;

namespace KikoleSite.Repositories;

/// <summary>
/// Dapper 2.1.28 n'a aucun support natif de <see cref="DateOnly"/> (verifie empiriquement :
/// sans ce handler, lire une colonne <c>DATE</c> en <see cref="DateOnly"/>/<see cref="DateOnly"/>?
/// renvoie silencieusement <c>null</c>/<c>default</c> sans la moindre exception, et passer un
/// <see cref="DateOnly"/> en parametre leve un <see cref="NotSupportedException"/>).
/// MySqlConnector lui-meme gere <see cref="DateOnly"/> (<c>GetFieldValue&lt;DateOnly&gt;</c>),
/// mais Dapper passe par <c>IDataRecord.GetValue</c>, qui renvoie un <see cref="DateTime"/>
/// pour une colonne <c>DATE</c> : ce handler fait la conversion manquante dans les deux sens.
/// Enregistre une seule fois au demarrage (cf. <c>Program.cs</c>), pour <see cref="DateOnly"/>
/// et <see cref="DateOnly"/>? separement (Dapper ne deduit pas l'un de l'autre).
/// </summary>
public sealed class DateOnlyTypeHandler : SqlMapper.TypeHandler<DateOnly>
{
    public override DateOnly Parse(object value) => DateOnly.FromDateTime((DateTime)value);

    public override void SetValue(IDbDataParameter parameter, DateOnly value) =>
        parameter.Value = value.ToDateTime(TimeOnly.MinValue);
}
