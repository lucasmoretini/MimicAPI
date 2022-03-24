using API.Helpers;
using API.V1.Models.DTO;
using API.V1.Repositories.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MimicAPI.V1.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MimicAPI.V1.Controllers
{
    //  /api/v1.0/palavras

    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/palavras")]
    public class PalavrasController : ControllerBase
    {
        private readonly IPalavraRepository _repository;
        private readonly IMapper _mapper;

        public PalavrasController(IPalavraRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }
        
        [HttpGet("/message")]
        public IActionResult Metodo()
        {
            return Ok(new { message = "ok" });
        }


        [MapToApiVersion("1.0")]
        //APP - /api/palavras?data=2019-05-01
        [HttpGet("", Name = "ObterTodas")]
        public ActionResult ObterTodas([FromQuery] PalavraUrlQuery query)
        {
            PaginationList<Palavra> item = _repository.ObterPalavras(query);

            if (item.Results.Count == 0)
                return NotFound();

            PaginationList<PalavraDTO> lista = _mapper.Map<PaginationList<Palavra>, PaginationList<PalavraDTO>>(item);

            CriarLinksListPalavraDTO(query, item, lista);
            
            return Accepted(lista);
        }

        private void CriarLinksListPalavraDTO(PalavraUrlQuery query, PaginationList<Palavra> item, PaginationList<PalavraDTO> lista)
        {
            foreach (PalavraDTO palavra in lista.Results)
            {
                palavra.Links = new List<LinkDTO>();
                palavra.Links.Add(
                    new LinkDTO("self", Url.Link("AtualizarPalavra", new { id = palavra.Id }), "PUT")
                );
            }

            lista.Links.Add(new LinkDTO("self", Url.Link("ObterTodas", query), "GET"));

            if (item.Paginacao != null)
            {
                Response.Headers.Add("X-Pagination", JsonConvert.SerializeObject(item.Paginacao));

                if (query.PagNumero + 1 <= item.Paginacao.TotalPaginas)
                {
                    var queryString = new PalavraUrlQuery() { PagNumero = query.PagNumero + 1, PagRegistro = query.PagRegistro, Data = query.Data };
                    lista.Links.Add(new LinkDTO("next", Url.Link("ObterTodas", queryString), "GET"));
                }

                if (query.PagNumero - 1 > 0)
                {
                    var queryString = new PalavraUrlQuery() { PagNumero = query.PagNumero - 1, PagRegistro = query.PagRegistro, Data = query.Data };
                    lista.Links.Add(new LinkDTO("prev", Url.Link("ObterTodas", queryString), "GET"));
                }

            }
        }


        [MapToApiVersion("1.0")]
        //Web -- /api/palavras/1
        [HttpGet("{id}", Name = "ObterPalavra")]
        public ActionResult Obter(int id)
        {
            Palavra obj = _repository.Obter(id);

            if (obj == null)
                return NotFound();


            PalavraDTO palavraDTO = _mapper.Map<Palavra, PalavraDTO>(obj);

            //pega o domínio da página corretamente. 
            palavraDTO.Links.Add(
                new LinkDTO("self", Url.Link("ObterPalavra", new { id = palavraDTO.Id }), "GET")
            );
            palavraDTO.Links.Add(
               new LinkDTO("self", Url.Link("AtualizarPalvra", new { id = palavraDTO.Id }), "PUT")
           );
            palavraDTO.Links.Add(
                new LinkDTO("delete", Url.Link("DeletarPalavra", new { id = palavraDTO.Id }), "DELETE")
            );


            return Ok(palavraDTO);
        }


        [MapToApiVersion("1.0")]
        // -- /api/palavras(Post: id, nome, ativo, pontuacao, criacao)
        [HttpPost("")]
        public ActionResult Cadastrar([FromBody] Palavra palavra)
        {
            if (palavra == null)
                return BadRequest(); //Code HTTP = 400

            if (!ModelState.IsValid)
                return UnprocessableEntity(ModelState); //Code HTTP = 422

            palavra.Ativo = true;
            palavra.Criado = DateTime.Now;

            _repository.Cadastrar(palavra);

            PalavraDTO palavraDTO = _mapper.Map<Palavra, PalavraDTO>(palavra);
            palavraDTO.Links.Add(
               new LinkDTO("self", Url.Link("ObterPalavra", new { id = palavraDTO.Id }), "GET")
           );

            return Created($"/api/palavras/{palavra.Id}", palavra);
        }


        [MapToApiVersion("1.0")]
        // -- /api/palavras/1 (PUT: id, nome, ativo, pontuacao, criacao)
        [HttpPut("{id}", Name = "AtualizarPalavra")]
        public ActionResult Atualizar([FromBody] int id, Palavra palavra)
        {

            Palavra obj = _repository.Obter(id);
            if (obj == null)
                return NotFound();

            if (palavra == null)
                return BadRequest(); //Code HTTP = 400

            if (!ModelState.IsValid)
                return UnprocessableEntity(ModelState); //Code HTTP = 422

            palavra.Id = id;
            palavra.Ativo = obj.Ativo;
            palavra.Criado = obj.Criado;
            palavra.Atualizado = DateTime.Now;
            _repository.Atualizar(palavra);

            PalavraDTO palavraDTO = _mapper.Map<Palavra, PalavraDTO>(palavra);
            palavraDTO.Links.Add(
                new LinkDTO("self", Url.Link("ObterPalavra", new { id = palavraDTO.Id }), "GET")
            );

            return Ok();
        }


        [MapToApiVersion("1.0")]
        // -- /api/palavras/1 (DELETE)
        [HttpDelete("{id}", Name = "ExcluirPalavra")]
        public ActionResult Deletar(int id)
        {
            var palavra = _repository.Obter(id);

            if (palavra == null)
                return NotFound();
            else
                _repository.Deletar(id);

            return NoContent();
        }
    }
}
