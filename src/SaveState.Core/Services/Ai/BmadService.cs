using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SaveState.Core.Services.Ai
{
    /// <summary>
    /// BMAD (Build More, Architect Dreams) Agent Framework
    /// Implements the 4-phase methodology from: https://github.com/bmad-code-org/BMAD-METHOD
    /// 
    /// 4 Phases:
    /// 1. Analysis - Research, brainstorm, explore
    /// 2. Planning - Create specs and requirements
    /// 3. Solutioning - Design architecture and approach
    /// 4. Implementation - Story-driven development
    /// 
    /// Specialized agents work together, each with domain expertise.
    /// </summary>

    public enum BmadPhase
    {
        Analysis,       // Research and exploration
        Planning,       // Requirements and specs
        Solutioning,    // Architecture and design
        Implementation  // Development and delivery
    }

    public enum AgentRole
    {
        // Analysis Phase
        Researcher,
        Analyst,
        
        // Planning Phase
        ProductManager,
        RequirementsEngineer,
        
        // Solutioning Phase
        Architect,
        UxDesigner,
        TechLead,
        
        // Implementation Phase
        Developer,
        Tester,
        DevOps,
        
        // Cross-cutting
        Orchestrator,
        QualityAssurance
    }

    public class BmadAgent
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public AgentRole Role { get; set; }
        public string Expertise { get; set; } = string.Empty;
        public string Personality { get; set; } = string.Empty;
        public string SystemPrompt { get; set; } = string.Empty;
        public BmadPhase PrimaryPhase { get; set; }
        public List<string> Capabilities { get; set; } = new();
    }

    public class BmadTask
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public BmadPhase Phase { get; set; }
        public AgentRole AssignedTo { get; set; }
        public string Status { get; set; } = "pending"; // pending, in_progress, complete, blocked
        public string? Output { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public List<string> Dependencies { get; set; } = new();
    }

    public class BmadWorkflow
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<BmadTask> Tasks { get; set; } = new();
        public BmadPhase CurrentPhase { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public Dictionary<string, string> Context { get; set; } = new();
    }

    public class BmadService
    {
        private static BmadService? _instance;
        private readonly ILlmService _llmService;
        private readonly Dictionary<AgentRole, BmadAgent> _agents = new();
        private readonly List<BmadWorkflow> _workflows = new();

        public static BmadService? Instance => _instance;

        public BmadService(ILlmService llmService)
        {
            _llmService = llmService;
            InitializeAgents();
        }

        private void InitializeAgents()
        {
            // Analysis Phase Agents
            RegisterAgent(new BmadAgent
            {
                Name = "Riley Research",
                Role = AgentRole.Researcher,
                PrimaryPhase = BmadPhase.Analysis,
                Expertise = "Information gathering, competitive analysis, user research",
                Personality = "Curious, thorough, detail-oriented",
                SystemPrompt = "You are a research specialist. Gather comprehensive information, identify patterns, and provide actionable insights. Be thorough but concise.",
                Capabilities = new() { "web_search", "document_analysis", "data_synthesis" }
            });

            RegisterAgent(new BmadAgent
            {
                Name = "Alex Analyst",
                Role = AgentRole.Analyst,
                PrimaryPhase = BmadPhase.Analysis,
                Expertise = "Data analysis, trend identification, risk assessment",
                Personality = "Analytical, objective, systematic",
                SystemPrompt = "You are a data analyst. Evaluate information objectively, identify risks and opportunities, and provide clear recommendations based on evidence.",
                Capabilities = new() { "data_analysis", "risk_assessment", "trend_detection" }
            });

            // Planning Phase Agents
            RegisterAgent(new BmadAgent
            {
                Name = "Paula ProductManager",
                Role = AgentRole.ProductManager,
                PrimaryPhase = BmadPhase.Planning,
                Expertise = "Product requirements, user stories, roadmap planning",
                Personality = "Strategic, user-focused, prioritization expert",
                SystemPrompt = "You are a product manager. Define clear requirements, prioritize features based on user value, and create actionable specs. Focus on the 'why' behind features.",
                Capabilities = new() { "prd_creation", "user_stories", "prioritization" }
            });

            RegisterAgent(new BmadAgent
            {
                Name = "Reese Requirements",
                Role = AgentRole.RequirementsEngineer,
                PrimaryPhase = BmadPhase.Planning,
                Expertise = "Functional specs, acceptance criteria, technical requirements",
                Personality = "Precise, systematic, documentation-focused",
                SystemPrompt = "You are a requirements engineer. Create detailed, unambiguous specifications with clear acceptance criteria. Ensure requirements are testable and complete.",
                Capabilities = new() { "spec_writing", "acceptance_criteria", "requirements_tracing" }
            });

            // Solutioning Phase Agents
            RegisterAgent(new BmadAgent
            {
                Name = "Arthur Architect",
                Role = AgentRole.Architect,
                PrimaryPhase = BmadPhase.Solutioning,
                Expertise = "System design, patterns, scalability, tech stack selection",
                Personality = "Visionary, pragmatic, quality-focused",
                SystemPrompt = "You are a software architect. Design scalable, maintainable systems. Balance ideal solutions with practical constraints. Document decisions and trade-offs.",
                Capabilities = new() { "system_design", "pattern_selection", "tech_evaluation" }
            });

            RegisterAgent(new BmadAgent
            {
                Name = "Uma UxDesigner",
                Role = AgentRole.UxDesigner,
                PrimaryPhase = BmadPhase.Solutioning,
                Expertise = "User experience, interface design, accessibility",
                Personality = "Empathetic, creative, user-advocate",
                SystemPrompt = "You are a UX designer. Create intuitive, accessible interfaces that delight users. Advocate for user needs and ensure designs are inclusive.",
                Capabilities = new() { "wireframing", "user_flows", "accessibility_review" }
            });

            RegisterAgent(new BmadAgent
            {
                Name = "Terry TechLead",
                Role = AgentRole.TechLead,
                PrimaryPhase = BmadPhase.Solutioning,
                Expertise = "Technical leadership, code review, team coordination",
                Personality = "Collaborative, mentoring, quality-driven",
                SystemPrompt = "You are a tech lead. Guide technical decisions, ensure code quality, and coordinate development work. Balance speed with maintainability.",
                Capabilities = new() { "code_review", "tech_decisions", "team_coordination" }
            });

            // Implementation Phase Agents
            RegisterAgent(new BmadAgent
            {
                Name = "Dana Developer",
                Role = AgentRole.Developer,
                PrimaryPhase = BmadPhase.Implementation,
                Expertise = "Code implementation, debugging, optimization",
                Personality = "Pragmatic, efficient, problem-solver",
                SystemPrompt = "You are a senior developer. Write clean, efficient code. Follow best practices and patterns. Always consider edge cases and error handling.",
                Capabilities = new() { "coding", "debugging", "optimization" }
            });

            RegisterAgent(new BmadAgent
            {
                Name = "Tanya Tester",
                Role = AgentRole.Tester,
                PrimaryPhase = BmadPhase.Implementation,
                Expertise = "Test design, automation, quality assurance",
                Personality = "Meticulous, creative (for finding bugs), thorough",
                SystemPrompt = "You are a QA engineer. Design comprehensive test strategies, find edge cases, and ensure software quality. Think like a user who will break things.",
                Capabilities = new() { "test_design", "automation", "bug_hunting" }
            });

            // Orchestrator (cross-cutting)
            RegisterAgent(new BmadAgent
            {
                Name = "Otto Orchestrator",
                Role = AgentRole.Orchestrator,
                PrimaryPhase = BmadPhase.Analysis, // Works across all phases
                Expertise = "Workflow coordination, agent delegation, progress tracking",
                Personality = "Organized, decisive, adaptive",
                SystemPrompt = "You are the workflow orchestrator. Coordinate tasks between specialized agents, track progress, and ensure smooth project flow. Delegate to the right expert for each task.",
                Capabilities = new() { "task_routing", "progress_tracking", "workflow_management" }
            });
        }

        public void RegisterAgent(BmadAgent agent)
        {
            _agents[agent.Role] = agent;
        }

        public BmadAgent? GetAgent(AgentRole role)
        {
            return _agents.GetValueOrDefault(role);
        }

        public List<BmadAgent> GetAgentsForPhase(BmadPhase phase)
        {
            return _agents.Values.Where(a => a.PrimaryPhase == phase).ToList();
        }

        // Execute a task with the appropriate agent
        public async Task<string> ExecuteTaskAsync(BmadTask task)
        {
            var agent = GetAgent(task.AssignedTo);
            if (agent == null)
            {
                return "Error: No agent found for this task";
            }

            task.Status = "in_progress";

            var prompt = $@"TASK: {task.Title}
DESCRIPTION: {task.Description}
PHASE: {task.Phase}

Please complete this task according to your expertise as {agent.Name}.";

            var result = await _llmService.CompleteAsync(prompt, agent.SystemPrompt);

            task.Output = result;
            task.Status = "complete";
            task.CompletedAt = DateTime.Now;

            return result;
        }

        // Create a workflow from a goal
        public async Task<BmadWorkflow> CreateWorkflowAsync(string goal, string projectType = "feature")
        {
            var orchestrator = GetAgent(AgentRole.Orchestrator)!;
            
            var planPrompt = $@"Create a development workflow for this goal: {goal}
Project type: {projectType}

Break this down into tasks across the 4 BMAD phases:
1. Analysis - Research and exploration needed
2. Planning - Requirements and specifications
3. Solutioning - Architecture and design decisions
4. Implementation - Development tasks

For each task, specify: Title, Description, and which agent role should handle it.
Format as a structured list.";

            var plan = await _llmService.CompleteAsync(planPrompt, orchestrator.SystemPrompt);

            var workflow = new BmadWorkflow
            {
                Name = goal,
                Description = plan,
                StartedAt = DateTime.Now,
                CurrentPhase = BmadPhase.Analysis,
                Context = new() { { "goal", goal }, { "project_type", projectType } }
            };

            // Parse and create tasks (simplified - in production would parse LLM output)
            workflow.Tasks.AddRange(CreateDefaultTasks(goal));

            _workflows.Add(workflow);
            return workflow;
        }

        private List<BmadTask> CreateDefaultTasks(string goal)
        {
            return new List<BmadTask>
            {
                new() { Title = "Research", Description = $"Research requirements for: {goal}", Phase = BmadPhase.Analysis, AssignedTo = AgentRole.Researcher },
                new() { Title = "Analysis", Description = $"Analyze feasibility for: {goal}", Phase = BmadPhase.Analysis, AssignedTo = AgentRole.Analyst },
                new() { Title = "Requirements", Description = $"Define requirements for: {goal}", Phase = BmadPhase.Planning, AssignedTo = AgentRole.ProductManager },
                new() { Title = "Specifications", Description = $"Write technical specs for: {goal}", Phase = BmadPhase.Planning, AssignedTo = AgentRole.RequirementsEngineer },
                new() { Title = "Architecture", Description = $"Design architecture for: {goal}", Phase = BmadPhase.Solutioning, AssignedTo = AgentRole.Architect },
                new() { Title = "UX Design", Description = $"Design user experience for: {goal}", Phase = BmadPhase.Solutioning, AssignedTo = AgentRole.UxDesigner },
                new() { Title = "Implementation", Description = $"Implement: {goal}", Phase = BmadPhase.Implementation, AssignedTo = AgentRole.Developer },
                new() { Title = "Testing", Description = $"Test: {goal}", Phase = BmadPhase.Implementation, AssignedTo = AgentRole.Tester },
            };
        }

        // Quick task execution with automatic agent selection
        public async Task<string> QuickExecuteAsync(string task, BmadPhase phase)
        {
            var agents = GetAgentsForPhase(phase);
            var agent = agents.FirstOrDefault() ?? GetAgent(AgentRole.Developer)!;

            return await _llmService.CompleteAsync(task, agent.SystemPrompt);
        }

        // Get workflow status
        public List<BmadWorkflow> GetActiveWorkflows()
        {
            return _workflows.Where(w => w.CompletedAt == null).ToList();
        }

        public Dictionary<string, BmadAgent> GetAllAgents()
        {
            return _agents.ToDictionary(kvp => kvp.Key.ToString(), kvp => kvp.Value);
        }

        // Interactive session with an agent
        public async Task<string> ChatWithAgentAsync(AgentRole role, string message, Dictionary<string, string>? context = null)
        {
            var agent = GetAgent(role);
            if (agent == null) return "Agent not found";

            var contextStr = context != null 
                ? string.Join("\n", context.Select(kvp => $"{kvp.Key}: {kvp.Value}"))
                : "";

            var prompt = string.IsNullOrEmpty(contextStr) 
                ? message 
                : $"Context:\n{contextStr}\n\nUser: {message}";

            return await _llmService.CompleteAsync(prompt, agent.SystemPrompt);
        }
    }
}
