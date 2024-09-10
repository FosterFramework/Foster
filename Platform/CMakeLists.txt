cmake_minimum_required(VERSION 3.14)
project(FosterPlatform C)

# Set flag for building a universal binary on macOS 
if(APPLE)
	set(CMAKE_OSX_ARCHITECTURES "arm64;x86_64")
endif()

# Default to Release build for library
if(NOT CMAKE_BUILD_TYPE)
	set(CMAKE_BUILD_TYPE Release)
endif()

# Define target name
set (TARGET_NAME FosterPlatform)

add_library(${TARGET_NAME} SHARED
	include/foster_platform.h
	src/foster_platform.c
)

target_include_directories(${TARGET_NAME}
	PUBLIC $<BUILD_INTERFACE:${CMAKE_CURRENT_SOURCE_DIR}/include>
	PRIVATE $<BUILD_INTERFACE:${CMAKE_CURRENT_SOURCE_DIR}/src>
)

if (MSVC)
    # Set warning level 3
    target_compile_options(${TARGET_NAME} PRIVATE /W3)

    # Warnings as Errors Enabled
    target_compile_options(${TARGET_NAME} PRIVATE /WX)
endif()

if(WIN32)
    target_compile_definitions(${TARGET_NAME} PRIVATE _UNICODE UNICODE _CRT_SECURE_NO_WARNINGS)
    target_compile_definitions(${TARGET_NAME} PRIVATE NOMINMAX)
endif ()

# Get platform target
if (FOSTER_OVERRIDE_TARGET)
	set(FosterTarget ${FOSTER_OVERRIDE_TARGET})
elseif (APPLE)
	set(FosterTarget osx)
elseif (WIN32)
	set(FosterTarget win-x64)
elseif (UNIX)
	if(CMAKE_SYSTEM_PROCESSOR STREQUAL "aarch64")
		set(FosterTarget linux-arm64)
	elseif(CMAKE_SYSTEM_PROCESSOR STREQUAL "armv7l")
		set(FosterTarget linux-arm32)
	elseif(CMAKE_SYSTEM_PROCESSOR STREQUAL "armv6l")
		set(FosterTarget linux-arm32)
	else()
		set(FosterTarget linux-x64)
	endif()
else()
	message(FATAL_ERROR "Unknown Target!")
endif()

# Pull in SDL3 from Github, build it as a shared lib
if (NOT DEFINED FOSTER_SDL3_LIBS)
	include(FetchContent)
	set(FETCHCONTENT_QUIET FALSE)
	set(SDL_TEST_LIBRARY OFF)
	set(SDL_SHARED ON)
	set(SDL_STATIC OFF)

	FetchContent_Declare(
		SDL3
		GIT_REPOSITORY https://github.com/libsdl-org/sdl
		GIT_TAG 379aea5
		GIT_PROGRESS TRUE
	)
	FetchContent_MakeAvailable(SDL3)
	set(FOSTER_SDL3_LIBS SDL3-shared)
	set(FOSTER_SDL3_INCLUDE ${SDL3_SOURCE_DIRS}/include)
endif()

# link SDL3
target_include_directories(${TARGET_NAME} PRIVATE ${FOSTER_SDL3_INCLUDE})
target_link_libraries(${TARGET_NAME} PRIVATE ${FOSTER_SDL3_LIBS})

# Output main lib to the libs folder
set_target_properties(${TARGET_NAME}
	PROPERTIES
	ARCHIVE_OUTPUT_DIRECTORY "${CMAKE_CURRENT_SOURCE_DIR}/libs/${FosterTarget}"
	ARCHIVE_OUTPUT_DIRECTORY_RELEASE "${CMAKE_CURRENT_SOURCE_DIR}/libs/${FosterTarget}"
	LIBRARY_OUTPUT_DIRECTORY "${CMAKE_CURRENT_SOURCE_DIR}/libs/${FosterTarget}"
	LIBRARY_OUTPUT_DIRECTORY_RELEASE "${CMAKE_CURRENT_SOURCE_DIR}/libs/${FosterTarget}"
	RUNTIME_OUTPUT_DIRECTORY "${CMAKE_CURRENT_SOURCE_DIR}/libs/${FosterTarget}"
	RUNTIME_OUTPUT_DIRECTORY_RELEASE "${CMAKE_CURRENT_SOURCE_DIR}/libs/${FosterTarget}"
)

# output SDL3 to the libs folder
set_target_properties(SDL3-shared
	PROPERTIES
	ARCHIVE_OUTPUT_DIRECTORY "${CMAKE_CURRENT_SOURCE_DIR}/libs/${FosterTarget}"
	ARCHIVE_OUTPUT_DIRECTORY_RELEASE "${CMAKE_CURRENT_SOURCE_DIR}/libs/${FosterTarget}"
	LIBRARY_OUTPUT_DIRECTORY "${CMAKE_CURRENT_SOURCE_DIR}/libs/${FosterTarget}"
	LIBRARY_OUTPUT_DIRECTORY_RELEASE "${CMAKE_CURRENT_SOURCE_DIR}/libs/${FosterTarget}"
	RUNTIME_OUTPUT_DIRECTORY "${CMAKE_CURRENT_SOURCE_DIR}/libs/${FosterTarget}"
	RUNTIME_OUTPUT_DIRECTORY_RELEASE "${CMAKE_CURRENT_SOURCE_DIR}/libs/${FosterTarget}"
)


