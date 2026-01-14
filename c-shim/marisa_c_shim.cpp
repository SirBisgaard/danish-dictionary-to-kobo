#include "marisa_c_shim.h"
#include <marisa/keyset.h>
#include <marisa/trie.h>
#include <marisa/agent.h>
#include <cstring>

extern "C" {
    // Builder functions
    marisa_builder_t marisa_builder_new(void) {
        try {
            return reinterpret_cast<marisa_builder_t>(new marisa::Keyset());
        } catch (...) {
            return nullptr;
        }
    }

    int marisa_builder_push(
        marisa_builder_t builder,
        const unsigned char* key,
        int length)
    {
        if (!builder || !key || length < 0) return -1;
        auto* ks = reinterpret_cast<marisa::Keyset*>(builder);
        try {
            ks->push_back(reinterpret_cast<const char*>(key),
                          static_cast<size_t>(length));
            return 0;
        } catch (...) {
            return -1;
        }
    }

    marisa_trie_t marisa_builder_build(
        marisa_builder_t builder)
    {
        if (!builder) return nullptr;
        auto* ks = reinterpret_cast<marisa::Keyset*>(builder);
        marisa::Trie* trie = nullptr;
        try {
            trie = new marisa::Trie();
            trie->build(*ks);
        } catch (...) {
            delete trie;
            return nullptr;
        }
        return reinterpret_cast<marisa_trie_t>(trie);
    }

    void marisa_builder_destroy(marisa_builder_t builder) {
        delete reinterpret_cast<marisa::Keyset*>(builder);
    }

    // Trie functions
    void marisa_trie_save(marisa_trie_t trie, const char* path) {
        if (!trie || !path) return;
        auto* t = reinterpret_cast<marisa::Trie*>(trie);
        try {
            t->save(path);
        } catch (...) { }
    }

    marisa_trie_t marisa_trie_load(const char* path) {
        if (!path) return nullptr;
        marisa::Trie* trie = nullptr;
        try {
            trie = new marisa::Trie();
            trie->load(path);
            return reinterpret_cast<marisa_trie_t>(trie);
        } catch (...) {
            delete trie;
            return nullptr;
        }
    }

    size_t marisa_trie_num_keys(marisa_trie_t trie) {
        if (!trie) return 0;
        auto* t = reinterpret_cast<marisa::Trie*>(trie);
        try {
            return t->num_keys();
        } catch (...) {
            return 0;
        }
    }

    int marisa_trie_lookup(
        marisa_trie_t trie,
        marisa_agent_t agent)
    {
        if (!trie || !agent) return 0;
        auto* t = reinterpret_cast<marisa::Trie*>(trie);
        auto* a = reinterpret_cast<marisa::Agent*>(agent);
        try {
            return t->lookup(*a) ? 1 : 0;
        } catch (...) {
            return 0;
        }
    }

    int marisa_trie_reverse_lookup(
        marisa_trie_t trie,
        marisa_agent_t agent,
        size_t key_id)
    {
        if (!trie || !agent) return -1;
        auto* t = reinterpret_cast<marisa::Trie*>(trie);
        auto* a = reinterpret_cast<marisa::Agent*>(agent);
        try {
            a->set_query(key_id);
            t->reverse_lookup(*a);
            return 0;
        } catch (...) {
            return -1;
        }
    }

    void marisa_trie_destroy(marisa_trie_t trie) {
        delete reinterpret_cast<marisa::Trie*>(trie);
    }

    // Agent functions
    marisa_agent_t marisa_agent_new(void) {
        try {
            return reinterpret_cast<marisa_agent_t>(new marisa::Agent());
        } catch (...) {
            return nullptr;
        }
    }

    void marisa_agent_set_query(
        marisa_agent_t agent,
        const unsigned char* key,
        int length)
    {
        if (!agent || !key || length < 0) return;
        auto* a = reinterpret_cast<marisa::Agent*>(agent);
        try {
            a->set_query(reinterpret_cast<const char*>(key),
                        static_cast<size_t>(length));
        } catch (...) { }
    }

    int marisa_agent_get_key(
        marisa_agent_t agent,
        unsigned char* buffer,
        int buffer_size,
        int* out_length)
    {
        if (!agent || !buffer || buffer_size <= 0 || !out_length) return -1;
        auto* a = reinterpret_cast<marisa::Agent*>(agent);
        try {
            const marisa::Key& key = a->key();
            size_t len = key.length();
            if (len > static_cast<size_t>(buffer_size - 1)) {
                return -1; // Buffer too small
            }
            memcpy(buffer, key.ptr(), len);
            buffer[len] = '\0'; // Null terminate
            *out_length = static_cast<int>(len);
            return 0;
        } catch (...) {
            return -1;
        }
    }

    void marisa_agent_destroy(marisa_agent_t agent) {
        delete reinterpret_cast<marisa::Agent*>(agent);
    }
} 
