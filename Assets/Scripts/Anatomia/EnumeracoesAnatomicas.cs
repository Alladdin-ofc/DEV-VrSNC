namespace VRSNC.Anatomia
{
    // Nome funcional em português: Enumerações Anatômicas do SNC
    // Nome técnico do script: EnumeracoesAnatomicas
    // Função: define categorias usadas por InformacaoEstruturaAnatomica e GrupoAnatomico.
    // Relação com o protótipo: padroniza divisões anatômicas exibidas no painel e usadas na documentação.
    // Objetos da cena: não é componente de GameObject; é usado pelos scripts de metadados anatômicos.
    /// <summary>
    /// Lado ou divisão espacial de uma estrutura anatômica.
    /// </summary>
    public enum LadoAnatomico
    {
        NaoEspecificado,
        Esquerda,
        Direita,
        LinhaMedia,
        Bilateral,
        RevisaoNecessaria
    }

    /// <summary>
    /// Grandes divisões do sistema nervoso central usadas no projeto.
    /// </summary>
    public enum DivisaoAnatomicaPrincipal
    {
        Desconhecido,
        Cerebro,
        Telencefalo,
        Diencefalo,
        Cerebelo,
        TroncoEncefalico,
        SistemaVentricular,
        OutrosRevisao
    }

    /// <summary>
    /// Subdivisões anatômicas mais específicas para documentação do SNC.
    /// </summary>
    public enum SubdivisaoAnatomica
    {
        Desconhecido,
        CortexCerebral,
        FormacaoHipocampal,
        SistemaLimbico,
        GangliosDaBase,
        SistemaOlfatorio,
        SubstanciaBrancaComissurasETratos,
        AreaSeptalEProsencefaloBasal,
        Talamo,
        Hipotalamo,
        Epitalamo,
        Subtalamo,
        Cerebelo,
        Mesencefalo,
        Ponte,
        BulboMedulaOblonga,
        VentriculosLaterais,
        TerceiroVentriculo,
        AquedutoCerebral,
        QuartoVentriculo,
        PlexoCoroide,
        NaoClassificado,
        RevisaoAnatomicaNecessaria
    }

    /// <summary>
    /// Sistemas anatômicos/funcionais usados como agrupamento auxiliar.
    /// </summary>
    public enum SistemaAnatomico
    {
        NaoEspecificado,
        Cortical,
        Limbico,
        Motor,
        Sensorial,
        Olfatorio,
        Ventricular,
        SubstanciaBranca,
        GangliosDaBase,
        Diencefalico,
        TroncoEncefalico,
        Cerebelar,
        RevisaoNecessaria
    }

    /// <summary>
    /// Lobos corticais ou categorias equivalentes.
    /// </summary>
    public enum LoboCortical
    {
        Nenhum,
        Frontal,
        Parietal,
        Temporal,
        Occipital,
        Insula,
        Limbico,
        Occipitotemporal,
        RevisaoNecessaria
    }

    /// <summary>
    /// Estado de revisão do metadado anatômico.
    /// </summary>
    public enum EstadoRevisaoEstrutura
    {
        Classificado,
        NaoEmCatalogo,
        AusenteEmCena,
        DuplicadoNome,
        RequerRevisao
    }
}