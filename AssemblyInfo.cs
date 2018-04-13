#region License and Terms
//
// splitcsv - CSV Splitter Utility
// Copyright (c) 2012 Atif Aziz. All rights reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//    http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
#endregion

#region Imports

using System.Reflection;
using CLSCompliantAttribute = System.CLSCompliantAttribute;
using ComVisible = System.Runtime.InteropServices.ComVisibleAttribute;

#endregion

[assembly: AssemblyTitle("SplitCsvApp")]
[assembly: AssemblyDescription("CSV Splitter Utility")]
[assembly: AssemblyCompany("")]
[assembly: AssemblyProduct("SplitCsvApp")]
[assembly: AssemblyCopyright("Copyright (c) 2012, Atif Aziz. All rights reserved.")]
[assembly: AssemblyCulture("")]

#if DEBUG
[assembly: AssemblyConfiguration("DEBUG")]
#else
[assembly: AssemblyConfiguration("RELEASE")]
#endif

[assembly: CLSCompliant(true)]
[assembly: ComVisible(false)]
