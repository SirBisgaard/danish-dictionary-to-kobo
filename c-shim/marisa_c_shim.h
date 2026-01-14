#ifndef MARISA_C_SHIM_H
#define MARISA_C_SHIM_H

#include <stddef.h>
#ifdef __cplusplus
extern "C" {
#endif

typedef void* marisa_builder_t;
typedef void* marisa_trie_t;
typedef void* marisa_agent_t;

// Builder functions
marisa_builder_t marisa_builder_new(void);

int               marisa_builder_push(
                     marisa_builder_t builder,
                     const unsigned char* key,
                     int length);

marisa_trie_t     marisa_builder_build(
                     marisa_builder_t builder);

void              marisa_builder_destroy(
                     marisa_builder_t builder);

// Trie functions
void marisa_trie_save(
       marisa_trie_t trie,
       const char* path);

marisa_trie_t marisa_trie_load(
       const char* path);

size_t marisa_trie_num_keys(
       marisa_trie_t trie);

int marisa_trie_lookup(
       marisa_trie_t trie,
       marisa_agent_t agent);

int marisa_trie_reverse_lookup(
       marisa_trie_t trie,
       marisa_agent_t agent,
       size_t key_id);

void marisa_trie_destroy(
      marisa_trie_t trie);

// Agent functions
marisa_agent_t marisa_agent_new(void);

void marisa_agent_set_query(
       marisa_agent_t agent,
       const unsigned char* key,
       int length);

int marisa_agent_get_key(
       marisa_agent_t agent,
       unsigned char* buffer,
       int buffer_size,
       int* out_length);

void marisa_agent_destroy(
       marisa_agent_t agent);

#ifdef __cplusplus
}
#endif
#endif

