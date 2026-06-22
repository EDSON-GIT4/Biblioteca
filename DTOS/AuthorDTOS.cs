using System;

namespace LibraryAPI.DTOs
{
    public record CreateAuthorDTO
    {
        //O campo ID é gerado pelo banco
        public string? Name { get; init;}
        public DateTime BirthDate {get; init;}
    }

    public record UpdateAuthorDTO
    {
        //O campo ID é gerado pelo banco
        public string? Name { get; init;}
        public DateTime? BirthDate {get; init;}
        public bool? IsActive { get; init; } 
    }

    public record ResponseAuthorDTO
    {
        public int Id {get; init;}
        public string Name { get; init;} = string.Empty;
        public DateTime BirthDate {get; init;}
        public bool IsActive { get; init;} // Soft delete(opcional mostrar)
    }

    public record AuthorFilterDTO
    {
    public string? Name { get; init; }
    public int? BornAfterYear { get; init; }    //  Período
    public int? BornBeforeYear { get; init; }   //  Período
    public int? BirthMonth { get; init; }       //  Mês
    public bool? IsActive { get; init; }        //  Nullable (true/false/null)
    public bool? HasBooks { get; init; }        //  Filtro de livros
    public int? MinBooks { get; init; }         //  Quantidade
    public int? MaxBooks { get; init; }         //  Quantidade
    public DateTime? CreatedAfter { get; init; } //  Auditoria
    public DateTime? CreatedBefore { get; init; }//  Auditoria
    public string? OrderBy { get; init; } = "name";
    public string? OrderDirection { get; init; } = "asc";
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 10; 
    }
}